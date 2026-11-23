using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Memorandum.Application.Abstractions;
using Memorandum.Domain.Common;
using Memorandum.Tests.Infrastructure;
using Memorandum.Tests.Infrastructure.Api;
using StackExchange.Redis;

namespace Memorandum.Tests.Api;

/// <summary>
/// Сквозной сценарий смены пароля по HTTP: коды живут в настоящем Redis, письмо
/// перехватывает подменённый отправитель. Без Redis код не проверить, поэтому
/// тесты помечены категорией Redis и падают с подсказкой, если его нет.
/// </summary>
[Trait("Category", "Redis")]
public sealed class PasswordChangeEndpointTests : IAsyncLifetime
{
    private readonly CapturingEmailSender _emails = new();

    private TestApi? _api;

    public async Task InitializeAsync() => _api = await TestApi.StartAsync(_emails);

    public async Task DisposeAsync()
    {
        if (_api is not null)
            await _api.DisposeAsync();
    }

    private bool Enabled => _api is not null;

    [Fact]
    public async Task Request_for_unknown_email_returns_no_content_and_sends_nothing()
    {
        if (!Enabled)
            throw new InvalidOperationException("Приложение не поднято: нет соединения с Redis.");

        var session = await Session.StartAsync(_api!.Client);
        var email = $"nobody-{Guid.NewGuid():N}@example.ru";

        using var response = await session.PostAsync("/api/v1/auth/change-password", new { email });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(_emails.Sent);
    }

    [Fact]
    public async Task Request_with_invalid_email_returns_validation_problem()
    {
        if (!Enabled)
            throw new InvalidOperationException("Приложение не поднято: нет соединения с Redis.");

        var session = await Session.StartAsync(_api!.Client);

        using var response = await session.PostAsync(
            "/api/v1/auth/change-password",
            new { email = "нет-собаки" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("Email", out _));
    }

    [Fact]
    public async Task Full_flow_changes_password_and_rejects_old_one()
    {
        if (!Enabled)
            throw new InvalidOperationException("Приложение не поднято: нет соединения с Redis.");

        var session = await Session.StartAsync(_api!.Client);
        var email = $"user-{Guid.NewGuid():N}@example.ru";
        var userId = await session.RegisterAsync(email);
        await ResetCooldownAsync(userId);

        using var request = await session.PostAsync("/api/v1/auth/change-password", new { email });
        Assert.Equal(HttpStatusCode.NoContent, request.StatusCode);

        var code = Assert.Single(_emails.SentFor(email)).Code;

        using var confirm = await session.PostAsync(
            $"/api/v1/auth/change-password/{userId}?code={code}",
            new { newPassword = "brand-new-password" });

        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);

        // Старый пароль больше не подходит, новый — да: SecurityStamp сменился.
        var login = await Session.StartAsync(_api!.Client);
        Assert.Equal(HttpStatusCode.Unauthorized, await login.TryLoginAsync(email, "password123"));
        await login.LoginAsync(email, "brand-new-password");
    }

    [Fact]
    public async Task Confirm_without_antiforgery_token_is_rejected()
    {
        if (!Enabled)
            throw new InvalidOperationException("Приложение не поднято: нет соединения с Redis.");

        var session = await Session.StartAsync(_api!.Client);
        session.ClearCsrfToken();

        using var response = await session.PostAsync(
            "/api/v1/auth/change-password/1?code=123456",
            new { newPassword = "brand-new-password" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Confirm_with_wrong_code_returns_attempts_left()
    {
        if (!Enabled)
            throw new InvalidOperationException("Приложение не поднято: нет соединения с Redis.");

        var session = await Session.StartAsync(_api!.Client);
        var email = $"user-{Guid.NewGuid():N}@example.ru";
        var userId = await session.RegisterAsync(email);
        await ResetCooldownAsync(userId);

        using var request = await session.PostAsync("/api/v1/auth/change-password", new { email });
        Assert.Equal(HttpStatusCode.NoContent, request.StatusCode);

        var code = Assert.Single(_emails.SentFor(email)).Code;
        var wrong = code == "000000" ? "111111" : "000000";

        using var confirm = await session.PostAsync(
            $"/api/v1/auth/change-password/{userId}?code={wrong}",
            new { newPassword = "brand-new-password" });

        Assert.Equal(HttpStatusCode.BadRequest, confirm.StatusCode);

        using var body = JsonDocument.Parse(await confirm.Content.ReadAsStringAsync());
        var root = body.RootElement;

        Assert.Equal(ErrorCodes.ResetCodeInvalid, root.GetProperty("code").GetString());
        Assert.Equal(2, root.GetProperty("details").GetProperty("attemptsLeft").GetInt32());
    }

    [Fact]
    public async Task Repeat_request_within_cooldown_returns_too_many_requests()
    {
        if (!Enabled)
            throw new InvalidOperationException("Приложение не поднято: нет соединения с Redis.");

        var session = await Session.StartAsync(_api!.Client);
        var email = $"user-{Guid.NewGuid():N}@example.ru";
        var userId = await session.RegisterAsync(email);
        await ResetCooldownAsync(userId);

        using var first = await session.PostAsync("/api/v1/auth/change-password", new { email });
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        using var second = await session.PostAsync("/api/v1/auth/change-password", new { email });

        Assert.Equal(HttpStatusCode.TooManyRequests, second.StatusCode);

        using var body = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        Assert.Equal(ErrorCodes.ResetTooManyRequests, body.RootElement.GetProperty("code").GetString());
    }

    /// <summary>Кулдаун живёт в Redis и переживает тесты: перед сценарием его нужно снять.</summary>
    private static Task ResetCooldownAsync(long userId) => RedisTestServer.ClearCooldownAsync(userId);

}

/// <summary>
/// Приложение, поднятое с подменённым отправителем писем: сценарий проверяет
/// письмо, которое получил бы пользователь, а не внутренности сервиса.
/// </summary>
internal sealed class TestApi : IAsyncDisposable
{
    private readonly MemorandumApiFactory _factory;

    private TestApi(MemorandumApiFactory factory, HttpClient client)
    {
        _factory = factory;
        Client = client;
    }

    public HttpClient Client { get; }

    public static async Task<TestApi> StartAsync(IEmailSender emails)
    {
        var factory = new MemorandumApiFactory
        {
            ConfigureServices = services => services.AddSingleton(emails),
            Settings = new()
            {
                ["Database:Provider"] = "Postgres",
                ["Mock:Enabled"] = "true",
                ["PasswordReset:CodePepper"] = "test-pepper",
                ["Redis:Configuration"] = RedisTestServer.Configuration,
                // Проверяется логика кулдауна, а не ожидание в минуту.
                ["PasswordReset:ResendCooldown"] = "00:00:00.100",
                ["RateLimit:AuthPermitLimit"] = "1000",
                ["RateLimit:GlobalPermitLimit"] = "1000",
            },
        };

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
            AllowAutoRedirect = false,
        });

        await Task.Yield();
        return new TestApi(factory, client);
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _factory.DisposeAsync();
    }
}

/// <summary>Письма складываются в список: тест берёт из них код.</summary>
internal sealed class CapturingEmailSender : IEmailSender
{
    private readonly List<SentEmail> _sent = [];
    private readonly Lock _lock = new();

    public IReadOnlyList<SentEmail> Sent
    {
        get
        {
            lock (_lock)
                return [.. _sent];
        }
    }

    public IEnumerable<SentEmail> SentFor(string email) =>
        Sent.Where(item => string.Equals(item.Email, email, StringComparison.OrdinalIgnoreCase));

    public Task SendPasswordResetCodeAsync(
        string email,
        long userId,
        string code,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        lock (_lock)
            _sent.Add(new SentEmail(email, userId, code, expiresAt));

        return Task.CompletedTask;
    }

    public sealed record SentEmail(string Email, long UserId, string Code, DateTimeOffset ExpiresAt);
}
