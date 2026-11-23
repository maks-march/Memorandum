using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Memorandum.Domain.Common;
using Memorandum.Tests.Infrastructure;
using Memorandum.Tests.Infrastructure.Api;

namespace Memorandum.Tests.Api;

/// <summary>
/// Антифоржери без ручной работы на клиенте: токен приходит cookie, middleware сам
/// перекладывает его в заголовок. Сторонний сайт так сделать не может, поэтому
/// защита сохраняется — это и проверяется.
/// </summary>
public sealed class CsrfCookieTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _fixture;

    public CsrfCookieTests(ApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Post_with_only_cookie_succeeds_without_manual_header()
    {
        var session = await Session.StartAsync(_fixture.Client);

        // Rаботает как обычно: Session шлёт cookie, заголовок не нужен.
        var userId = await session.RegisterAsync($"cookie-{Guid.NewGuid():N}@example.ru");

        Assert.True(userId > 0);
    }

    [Fact]
    public async Task Post_without_cookie_is_rejected_with_explanation()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = Json("""{"email":"a@b.ru","password":"password123"}"""),
        };

        using var response = await _fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(ErrorCodes.AntiforgeryTokenInvalid, body.RootElement.GetProperty("code").GetString());
        Assert.Contains("csrf", body.RootElement.GetProperty("detail").GetString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cross_site_request_is_rejected_even_with_cookie()
    {
        // Клиент получил cookie, но запрос идёт «с чужого сайта»: браузер сам
        // проставил бы Sec-Fetch-Site: cross-site и приложил cookie.
        var session = await Session.StartAsync(_fixture.Client);
        var cookie = session.Cookies.Single(pair => pair.Key == "XSRF-TOKEN");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = Json("""{"email":"a@b.ru","password":"password123"}"""),
        };
        request.Headers.Add("Cookie", $"{cookie.Key}={cookie.Value}");
        request.Headers.Add("Sec-Fetch-Site", "cross-site");
        request.Headers.Add("Origin", "https://evil.example");

        using var response = await _fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(ErrorCodes.AntiforgeryTokenInvalid, body.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Csrf_endpoint_sets_readable_cookie()
    {
        using var response = await _fixture.Client.GetAsync("/api/v1/auth/csrf");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.StartsWith("XSRF-TOKEN=", cookie, StringComparison.Ordinal);
        Assert.DoesNotMatch(new Regex("httponly", RegexOptions.IgnoreCase), cookie);
    }

    private static StringContent Json(string body) => new(body, Encoding.UTF8, "application/json");
}
