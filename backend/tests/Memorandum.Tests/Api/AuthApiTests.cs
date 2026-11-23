using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Memorandum.Api.Contracts;

namespace Memorandum.Tests.Infrastructure.Api;

/// <summary>
/// HTTP-контракт auth: cookie-аутентификация, антифоржери и коды ошибок.
/// Приложение поднимается целиком (<c>WebApplicationFactory</c>) на Postgres.
/// </summary>
public sealed class AuthApiTests : IClassFixture<ApiFixture>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly ApiFixture _fixture;

    public AuthApiTests(ApiFixture fixture) => _fixture = fixture;

    private HttpClient Client => _fixture.Client;

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.ru";

    [Fact]
    public async Task Csrf_returns_token_and_readable_cookie()
    {
        using var response = await Client.GetAsync("/api/v1/auth/csrf");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.StartsWith("XSRF-TOKEN=", cookie, StringComparison.Ordinal);
        Assert.DoesNotContain("httponly", cookie, StringComparison.OrdinalIgnoreCase);

        var body = await response.Content.ReadFromJsonAsync<CsrfHttpResponse>(Json);
        Assert.False(string.IsNullOrWhiteSpace(body!.XsrfToken));
    }

    [Fact]
    public async Task Register_without_antiforgery_token_is_rejected()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/register")
        {
            Content = JsonContent.Create(new RegisterHttpRequest(UniqueEmail(), "password123", "Иван", null)),
        };

        using var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Register_with_organization_creates_account_and_conflicts_on_duplicate()
    {
        var session = await Session.StartAsync(Client);
        var email = UniqueEmail();

        using var created = await session.PostAsync(
            "/api/v1/auth/register",
            new RegisterHttpRequest(email, "password123", "Иван Петров", "ООО Ромашка"));

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(created.Headers.Location);

        using var duplicate = await session.PostAsync(
            "/api/v1/auth/register",
            new RegisterHttpRequest(email.ToUpperInvariant(), "password123", "Второй", null));

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var problem = JsonDocument.Parse(await duplicate.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("EMAIL_DUPLICATE", problem.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));

        await using var db = _fixture.NewContext();
        Assert.Equal(1, await db.Users.CountAsync(user => user.NormalizedEmail == email.ToLowerInvariant()));
    }

    [Fact]
    public async Task Register_validates_form_fields()
    {
        var session = await Session.StartAsync(Client);

        using var response = await session.PostAsync(
            "/api/v1/auth/register",
            new RegisterHttpRequest("нет-собаки", "short", " ", null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var errors = problem.GetProperty("errors");
        Assert.True(errors.TryGetProperty("Email", out _));
        Assert.True(errors.TryGetProperty("Password", out _));
        Assert.True(errors.TryGetProperty("FullName", out _));
    }

    [Fact]
    public async Task Login_returns_profile_and_me_works_with_cookie()
    {
        var session = await Session.StartAsync(Client);
        var email = UniqueEmail();
        await session.RegisterAsync(email, "ООО Ромашка");

        var login = await session.LoginAsync(email);

        Assert.Equal(email, login.User.Email);
        Assert.Equal("ООО Ромашка", login.User.OrganizationName);
        Assert.Equal(1, (int)login.User.Role!.Value);
        Assert.False(string.IsNullOrWhiteSpace(login.XsrfToken));
        Assert.NotNull(session.AuthCookieName);

        using var me = await session.GetAsync("/api/v1/me");

        Assert.Equal(HttpStatusCode.OK, me.StatusCode);

        var profile = await me.Content.ReadFromJsonAsync<MeHttpResponse>(Json);
        Assert.Equal(email, profile!.Email);
        Assert.Equal("ООО Ромашка", profile.OrganizationName);
        Assert.Equal(1, (int)profile.Role!.Value);
    }

    [Fact]
    public async Task Me_returns_nulls_without_organization()
    {
        var session = await Session.StartAsync(Client);
        var email = UniqueEmail();
        await session.RegisterAsync(email);
        await session.LoginAsync(email);

        using var me = await session.GetAsync("/api/v1/me");
        var profile = await me.Content.ReadFromJsonAsync<MeHttpResponse>(Json);

        Assert.Null(profile!.OrganizationId);
        Assert.Null(profile.OrganizationName);
        Assert.Null(profile.Role);
    }

    [Fact]
    public async Task Login_does_not_reveal_reason_of_failure()
    {
        var session = await Session.StartAsync(Client);
        var email = UniqueEmail();
        await session.RegisterAsync(email);

        using var wrongPassword = await session.PostAsync(
            "/api/v1/auth/login", new LoginHttpRequest(email, "wrong-password"));
        using var unknownUser = await session.PostAsync(
            "/api/v1/auth/login", new LoginHttpRequest(UniqueEmail(), "password123"));

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownUser.StatusCode);

        var first = JsonDocument.Parse(await wrongPassword.Content.ReadAsStringAsync()).RootElement;
        var second = JsonDocument.Parse(await unknownUser.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(first.GetProperty("detail").GetString(), second.GetProperty("detail").GetString());
        Assert.Equal(first.GetProperty("code").GetString(), second.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Login_requires_antiforgery_token()
    {
        using var response = await Client.PostAsJsonAsync(
            "/api/v1/auth/login", new LoginHttpRequest("a@b.ru", "password123"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Me_without_cookie_returns_problem_401()
    {
        using var response = await Client.GetAsync("/api/v1/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Logout_clears_session()
    {
        var session = await Session.StartAsync(Client);
        var email = UniqueEmail();
        await session.RegisterAsync(email);
        await session.LoginAsync(email);

        using var meBefore = await session.GetAsync("/api/v1/me");
        Assert.Equal(HttpStatusCode.OK, meBefore.StatusCode);

        using var logout = await session.PostAsync("/api/v1/auth/logout", body: null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        using var meAfter = await session.GetAsync("/api/v1/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meAfter.StatusCode);
    }

    [Fact]
    public async Task Logout_without_antiforgery_token_is_rejected()
    {
        var session = await Session.StartAsync(Client);
        var email = UniqueEmail();
        await session.RegisterAsync(email);
        await session.LoginAsync(email);

        session.ClearCsrfToken();

        using var response = await session.PostAsync("/api/v1/auth/logout", body: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Deactivated_user_loses_access_with_old_cookie()
    {
        var session = await Session.StartAsync(Client);
        var email = UniqueEmail();
        var userId = await session.RegisterAsync(email);
        await session.LoginAsync(email);

        await using (var db = _fixture.NewContext())
        {
            var user = await db.Users.SingleAsync(candidate => candidate.Id == userId);
            user.Deactivate(DateTime.UtcNow, "admin");
            await db.SaveChangesAsync();
        }

        using var response = await session.GetAsync("/api/v1/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
