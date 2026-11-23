using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Memorandum.Application.Abstractions;
using Memorandum.Domain.Common;
using Memorandum.Tests.Infrastructure;
using Memorandum.Tests.Infrastructure.Api;

namespace Memorandum.Tests.Api;

/// <summary>
/// Упавший Redis не должен превращаться в 500 с трейсом: это временная проблема
/// инфраструктуры, клиенту нужен код STORAGE_UNAVAILABLE и статус 503.
/// Настоящий Redis здесь не нужен — хранилище подменяется «мёртвым» адресом.
/// </summary>
public sealed class RedisUnavailableTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _fixture;

    public RedisUnavailableTests(ApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Dead_redis_returns_503_and_storage_code_instead_of_500()
    {
        await using var store = new UnavailableRedisTicketStore();
        // База — от общего стенда: регистрируем её строку через конфигурацию,
        // чтобы не плодить лишнюю базу ради одного теста.
        using var factory = new MemorandumApiFactory
        {
            ConfigureServices = services => services.AddSingleton<IPasswordResetTicketStore>(store),
            Settings = new()
            {
                ["Database:Provider"] = "Postgres",
                ["ConnectionStrings:Postgres"] = _fixture.ConnectionString,
                ["Mock:Enabled"] = "true",
                ["PasswordReset:CodePepper"] = "test-pepper",
                ["Invitations:CodePepper"] = "test-pepper",
                ["Redis:Configuration"] = "localhost:6379,abortConnect=false,connectTimeout=1000",
                ["RateLimit:AuthPermitLimit"] = "1000",
                ["RateLimit:GlobalPermitLimit"] = "1000",
            },
        };

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var session = await Session.StartAsync(client);

        // Пользователь должен существовать: до Redis дело доходит только для известного email.
        var email = $"redis-{Guid.NewGuid():N}@example.ru";
        await session.RegisterAsync(email);

        using var response = await session.PostAsync("/api/v1/auth/change-password", new { email });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var codes = new[]
        {
            body.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null,
            body.RootElement.TryGetProperty("detail", out _) ? "detail" : null,
        };

        Assert.Equal(ErrorCodes.StorageUnavailable, codes[0]);
        Assert.Equal("detail", codes[1]);
    }
}
