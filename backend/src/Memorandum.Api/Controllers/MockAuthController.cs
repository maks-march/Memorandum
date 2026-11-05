using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Memorandum.Api.Mocks;
using Memorandum.Api.Contracts;

namespace Memorandum.Api.Controllers;

/// <summary>
/// Мок авторизации. Существует только в Development.
/// </summary>
[ApiController]
[ApiVersionNeutral]
[Route("mock/api")]
[Tags("Auth (mock)")]
[DevelopmentOnly]
internal sealed class MockAuthController(MockAuthStore store) : ControllerBase
{
    internal const string AuthCookie = "memorandum.auth";
    internal const string XsrfCookie = "XSRF-TOKEN";
    internal const string XsrfHeader = "X-XSRF-TOKEN";

    [HttpPost("auth/login")]
    public IActionResult Login(LoginRequest request)
    {
        var user = store.FindByEmail(request.Email);
        if (user is null || !store.CheckPassword(user, request.Password))
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Неверный email или пароль");

        var (sid, xsrf) = store.CreateSession(user);
        SetAuthCookies(sid, xsrf);

        return Ok(new LoginResponse(ToMe(user), xsrf));
    }

    [HttpPost("auth/logout")]
    public IActionResult Logout()
    {
        var sid = Request.Cookies[AuthCookie];
        if (sid is not null)
            store.RemoveSession(sid);

        Response.Cookies.Delete(AuthCookie);
        Response.Cookies.Delete(XsrfCookie);

        return NoContent();
    }

    [HttpGet("me")]
    public ActionResult<MeResponse> Me()
    {
        var user = CurrentUser();
        return user is null
            ? Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Требуется вход")
            : Ok(ToMe(user));
    }

    /// <summary>Пользователь из cookie или null, если сессии нет либо она истекла.</summary>
    private MockUser? CurrentUser()
    {
        var sid = Request.Cookies[AuthCookie];
        return sid is null ? null : store.GetUserBySession(sid);
    }

    private void SetAuthCookies(string sid, string xsrf)
    {
        Response.Cookies.Append(AuthCookie, sid, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Secure = Request.IsHttps,   // в dev по http иначе браузер выбросит cookie
            Expires = DateTimeOffset.UtcNow.AddHours(8)
        });

        // читаемая cookie: фронт берёт из неё токен и дублирует его заголовком
        Response.Cookies.Append(XsrfCookie, xsrf, new CookieOptions
        {
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Secure = Request.IsHttps
        });
    }

    private static MeResponse ToMe(MockUser user)
    {
        var mockOrg = user.OrgId is { } id
            ? MockAuthFixtures.Orgs().Where(o => o.Id == id).FirstOrDefault()
            : null;
        return new MeResponse(user.Id, user.Email, user.FullName, user.Position, user.Role, mockOrg);
    }
}
