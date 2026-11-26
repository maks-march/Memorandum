using Asp.Versioning;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Memorandum.Api.Contracts;
using Memorandum.Api.Infrastructure.RateLimiting;
using Memorandum.Api.Mapping;
using Memorandum.Application.PasswordChange;
using Memorandum.Application.PasswordChange.Models;

namespace Memorandum.Api.Controllers.Auth;

/// <summary>Смена пароля по коду.</summary>
/// <remarks>Оба шага анонимные: пользователь ещё не вошёл.</remarks>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth/change-password")]
[Tags("Auth")]
public sealed class PasswordChangeController(IPasswordChangeService service) : ControllerBase
{
    /// <summary>Запрос кода на email.</summary>
    /// <remarks>Ответ одинаков для известного и неизвестного адреса.</remarks>
    /// <response code="204">Код отправлен (или адрес неизвестен — ответ тот же).</response>
    /// <response code="400">Некорректный email.</response>
    /// <response code="429">Код уже отправлен недавно; повторите позже.</response>
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> RequestCode(
        [FromBody] RequestPasswordChangeCodeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.RequestCodeAsync(request, cancellationToken);

        return result.IsSuccess ? NoContent() : result.Error.ToProblem(this);
    }

    /// <summary>Подтверждение кода новым паролём.</summary>
    /// <response code="204">Пароль изменён; ранее выданные сессии отозваны сменой SecurityStamp.</response>
    /// <response code="400">Неверный или истёкший код, попытки исчерпаны, невалидный пароль.</response>
    [HttpPost("{userId:long}")]
    [AllowAnonymous]
    [RequireAntiforgeryToken]
    [EnableRateLimiting(RateLimitingExtensions.AuthPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Confirm(
        long userId,
        [FromQuery] string? code,
        [FromBody] ConfirmPasswordChangeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.ConfirmAsync(userId, code, request, cancellationToken);

        return result.IsSuccess ? NoContent() : result.Error.ToProblem(this);
    }
}
