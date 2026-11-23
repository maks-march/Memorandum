using Asp.Versioning;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Memorandum.Api.Infrastructure.RateLimiting;
using Memorandum.Api.Auth;
using Memorandum.Api.Contracts;
using Memorandum.Api.Mapping;
using Memorandum.Application.Auth;
using Memorandum.Application.CurrentUser;
using Memorandum.Application.Invitations;
using Memorandum.Application.Invitations.Models;

namespace Memorandum.Api.Controllers.Auth;

/// <summary>Регистрация, вход, выход и выдача анти-CSRF токена.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[Tags("Auth")]
public sealed class AuthController(
    IAuthService authService,
    ICurrentUserService currentUserService,
    IInviteCodeService inviteCodeService,
    IAntiforgery antiforgery) : ControllerBase
{
    /// <summary>Анти-CSRF токен. Нужен до первого POST, поэтому доступен без входа.</summary>
    /// <response code="200">Токен выдан; cookie <c>XSRF-TOKEN</c> установлена.</response>
    [HttpGet("csrf")]
    [AllowAnonymous]
    [ProducesResponseType<CsrfHttpResponse>(StatusCodes.Status200OK)]
    public ActionResult<CsrfHttpResponse> GetCsrfToken() => Ok(new CsrfHttpResponse(IssueAntiforgeryToken()));

    /// <summary>
    /// Регистрация. Вход не выполняет: cookie выдаёт <c>login</c>.
    /// С <c>orgName</c> пользователь создаёт организацию и становится её администратором —
    /// это единственный путь получить организацию без кода приглашения.
    /// </summary>
    /// <response code="201">Пользователь создан; заголовок Location ведёт на профиль.</response>
    /// <response code="400">Некорректные поля запроса.</response>
    /// <response code="409">Email уже зарегистрирован.</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [RequireAntiforgeryToken]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    [ProducesResponseType<MeHttpResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterHttpRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(
            new RegisterRequest(request.Email, request.Password, request.FullName, request.OrgName),
            cancellationToken);

        if (result.IsFailure)
            return result.Error.ToProblem(this);

        var body = new MeHttpResponse(
            result.Value.Id, result.Value.Email, result.Value.FullName,
            Phone: null, Position: null, OrganizationId: null, OrganizationName: null, Role: null);

        // Location ведёт на профиль, хотя сам профиль вернётся только после входа.
        return Created($"/api/v1/me", body);
    }

    /// <summary>
    /// Регистрация сотрудника по коду приглашения. Организация и роль берутся из кода,
    /// код одноразовый. Вход, как и у обычной регистрации, отдельным вызовом.
    /// </summary>
    /// <response code="201">Сотрудник создан и добавлен в организацию.</response>
    /// <response code="400">Некорректные поля или код не указан.</response>
    /// <response code="409">Email уже зарегистрирован.</response>
    /// <response code="422">Код недействителен, истёк или уже использован.</response>
    [HttpPost("accept-invite")]
    [AllowAnonymous]
    [RequireAntiforgeryToken]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    [ProducesResponseType<MeHttpResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AcceptInvite(
        [FromBody] AcceptInviteHttpRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.RegisterByInviteAsync(
            new RegisterRequest(request.Email, request.Password, request.FullName, OrgName: null),
            request.Code,
            cancellationToken);

        if (result.IsFailure)
            return result.Error.ToProblem(this);

        var body = new MeHttpResponse(
            result.Value.Id, result.Value.Email, result.Value.FullName,
            Phone: null, Position: null, OrganizationId: null, OrganizationName: null, Role: null);

        return Created($"/api/v1/me", body);
    }

    /// <summary>
    /// Код приглашения в свою организацию. Выдаёт только администратор организации;
    /// новый код отменяет предыдущий, срок жизни — сутки, в хранилище лежит лишь хеш.
    /// </summary>
    /// <response code="200">Код выдан — показывается один раз.</response>
    /// <response code="400">Нельзя выдать такую роль.</response>
    /// <response code="401">Нет cookie или сессия отозвана.</response>
    /// <response code="403">Пользователь не администратор организации.</response>
    /// <response code="503">Хранилище кодов недоступно.</response>
    [HttpPost("invite-code")]
    [Authorize]
    [RequireAntiforgeryToken]
    [ProducesResponseType<InviteCodeHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> RequestInviteCode(
        [FromBody] RequestInviteCodeHttpRequest request,
        CancellationToken cancellationToken)
    {
        var result = await inviteCodeService.IssueAsync(
            new RequestInviteCodeRequest(request.Role),
            cancellationToken);

        return result.IsSuccess
            ? Ok(InviteCodeHttpResponse.From(result.Value))
            : result.Error.ToProblem(this);
    }

    /// <summary>Вход по email и паролю. Ошибка не уточняет, что именно неверно.</summary>
    /// <response code="200">Cookie сессии и профиль.</response>
    /// <response code="401">Неверный email или пароль, либо учётная запись заблокирована.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [RequireAntiforgeryToken]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    [ProducesResponseType<LoginHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginHttpRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(new LoginRequest(request.Email, request.Password), cancellationToken);

        if (result.IsFailure)
            return result.Error.ToProblem(this);

        var principal = ClaimsPrincipalFactory.Create(result.Value);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = false });

        // SignInAsync ставит cookie, но не меняет User текущего запроса: без этого
        // профиль ниже считался бы анонимным.
        HttpContext.User = principal;

        var profile = await currentUserService.GetAsync(cancellationToken);

        if (profile.IsFailure)
            return profile.Error.ToProblem(this);

        return Ok(new LoginHttpResponse(
            MeHttpResponse.From(profile.Value),
            IssueAntiforgeryToken()));
    }

    /// <summary>Выход: cookie сессии удаляется.</summary>
    /// <response code="204">Сессия завершена.</response>
    [HttpPost("logout")]
    [Authorize]
    [RequireAntiforgeryToken]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    /// <summary>
    /// Токен в теле и в cookie: после смены принципала (вход, выход) cookie антифоржери
    /// привязывается к другому пользователю, токен нужно перевыпустить.
    /// </summary>
    private string IssueAntiforgeryToken()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return tokens.RequestToken!;
    }
}
