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
public sealed class MockAuthController(MockAuthStore store) : ControllerBase
{
    private static UserDto ToDto(MockUser user) => new(
        user.Id,
        user.Email,
        user.FullName,
        user.Position,
        user.Role,
        user.OrgId
    );
    
    [HttpPost("login")]
    public ActionResult<UserDto> Login([FromBody] LoginRequest request)
    {
        var result =
            from user in store.Authenticate(request.Email, request.Password)
            from session in store.CreateSession(user)
            select (user, session);

        result.IfSucc(pair =>
        {
            AppendSessionCookie(pair.session);
            Response.Headers["X-XSRF-TOKEN"] = pair.session.XsrfToken;
        });

        return result
            .ToActionResult(pair => ToDto(pair.user));
    }

    [HttpGet("me")]
    public ActionResult<UserDto> Me() =>
        store.RequireUser(Request.Cookies["sid"]).ToActionResult(mock => ToDto(mock));

    private void AppendSessionCookie(MockSession session) =>
        Response.Cookies.Append("sid", session.sessionId, 
        new CookieOptions
        {
            HttpOnly = false,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            MaxAge = TimeSpan.FromHours(8),
        });

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        var sessionId = Request.Cookies["sid"] ?? string.Empty;
        var xsrf = store.RequireXsrf(sessionId, Request.Headers["X-XSRF-TOKEN"]);

        if (xsrf.IsFail)
            return xsrf.ToNoContentResult();

        store.RemoveSession(sessionId);
        Response.Cookies.Delete("sid");
        return NoContent();
    }
}
