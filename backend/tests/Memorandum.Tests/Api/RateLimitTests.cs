using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Memorandum.Api.Contracts;

namespace Memorandum.Tests.Infrastructure.Api;

/// <summary>
/// Ограничение частоты: перебор пароля упирается в 429 с Retry-After и ProblemDetails,
/// а «соседний» клиент продолжает работать.
/// </summary>
public sealed class RateLimitTests : IAsyncLifetime
{
    private MemorandumApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new MemorandumApiFactory
        {
            Settings = new()
            {
                ["Database:Provider"] = "Sqlite",
                ["RateLimit:AuthPermitLimit"] = "3",
                ["RateLimit:AuthWindowSeconds"] = "60",
            },
        };

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Auth_requests_are_limited_per_ip_with_retry_after()
    {
        var session = await Session.StartAsync(_client);

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            using var allowed = await session.PostAsync(
                "/api/v1/auth/login", new LoginHttpRequest("nobody@example.ru", "password123"));

            Assert.Equal(HttpStatusCode.Unauthorized, allowed.StatusCode);
        }

        using var rejected = await session.PostAsync(
            "/api/v1/auth/login", new LoginHttpRequest("nobody@example.ru", "password123"));

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Equal("application/problem+json", rejected.Content.Headers.ContentType?.MediaType);
        Assert.True(rejected.Headers.RetryAfter?.Delta > TimeSpan.Zero);

        var problem = JsonDocument.Parse(await rejected.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("TOO_MANY_REQUESTS", problem.GetProperty("code").GetString());
        Assert.Equal(429, problem.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Rate_limit_does_not_block_csrf_and_profile()
    {
        var session = await Session.StartAsync(_client);

        for (var attempt = 1; attempt <= 4; attempt++)
        {
            using var _ = await session.PostAsync(
                "/api/v1/auth/login", new LoginHttpRequest("nobody@example.ru", "password123"));
        }

        // Лимит исчерпан только для POST login: чтение токена и /me остаются доступными.
        using var csrf = await _client.GetAsync("/api/v1/auth/csrf");
        using var me = await _client.GetAsync("/api/v1/me");

        Assert.Equal(HttpStatusCode.OK, csrf.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }
}
