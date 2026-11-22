using System.Net;
using System.Text;
using System.Text.Json;
using Memorandum.Domain.Common;
using Memorandum.Tests.Infrastructure;
using Memorandum.Tests.Infrastructure.Api;

namespace Memorandum.Tests.Api;

/// <summary>
/// Провал антифоржери должен объяснять причину: стандартный «400 Bad Request»
/// без кода не даёт фронту понять, перевыпускать токен или отправить его вообще.
/// </summary>
public sealed class AntiforgeryTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _fixture;

    public AntiforgeryTests(ApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Missing_token_reports_that_token_is_not_sent()
    {
        using var request = LoginRequest(HttpMethod.Post);
        request.Content = LoginBody();

        using var response = await _fixture.Client.SendAsync(request);

        var problem = await ReadProblemAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ErrorCodes.AntiforgeryTokenInvalid, problem.Code);
        Assert.Contains("не передан", problem.Detail, StringComparison.Ordinal);
        Assert.Contains("/api/v1/auth/csrf", problem.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invalid_token_reports_mismatch_with_cookie()
    {
        // Настоящая cookie из /csrf, но заголовок подменён — токен не совпадает.
        var session = await Session.StartAsync(_fixture.Client);
        session.SetRawToken("подделка");

        using var response = await session.PostAsync(
            "/api/v1/auth/login",
            new { email = "a@b.ru", password = "password123" });

        var problem = await ReadProblemAsync(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(ErrorCodes.AntiforgeryTokenInvalid, problem.Code);
        Assert.Contains("не совпадает", problem.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Token_without_cookie_reports_missing_cookie()
    {
        using var request = LoginRequest(HttpMethod.Post);
        request.Content = LoginBody();
        request.Headers.Add("X-XSRF-TOKEN", "какой-то-токен");

        using var response = await _fixture.Client.SendAsync(request);

        var problem = await ReadProblemAsync(response);

        Assert.Equal(ErrorCodes.AntiforgeryTokenInvalid, problem.Code);
        Assert.Contains("Cookie XSRF-TOKEN", problem.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Validation_error_is_not_reported_as_token_problem()
    {
        var session = await Session.StartAsync(_fixture.Client);

        using var response = await session.PostAsync(
            "/api/v1/auth/login",
            new { email = "нет-собаки", password = "" });

        var problem = await ReadProblemAsync(response);

        Assert.NotEqual(ErrorCodes.AntiforgeryTokenInvalid, problem.Code);
    }

    private HttpRequestMessage LoginRequest(HttpMethod method) =>
        new(method, "/api/v1/auth/login");

    private static StringContent LoginBody() =>
        new("""{"email":"a@b.ru","password":"password123"}""", Encoding.UTF8, "application/json");

    private static async Task<(string Code, string Detail)> ReadProblemAsync(HttpResponseMessage response)
    {
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = body.RootElement;

        var code = root.TryGetProperty("code", out var codeElement) ? codeElement.GetString() ?? "" : "";
        var detail = root.TryGetProperty("detail", out var detailElement) ? detailElement.GetString() ?? "" : "";

        return (code, detail);
    }
}
