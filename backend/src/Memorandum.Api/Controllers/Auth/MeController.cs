using Asp.Versioning;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Memorandum.Api.Contracts;
using Memorandum.Api.Mapping;
using Memorandum.Application.CurrentUser;
using Memorandum.Application.CurrentUser.Models;

namespace Memorandum.Api.Controllers.Auth;

/// <summary>Профиль вошедшего пользователя.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/me")]
[Tags("Me")]
[Authorize]
public sealed class MeController(ICurrentUserService currentUserService) : ControllerBase
{
    /// <summary>Профиль: организация и роль приходят null, если пользователь ещё не состоит в организации.</summary>
    /// <response code="200">Профиль.</response>
    /// <response code="401">Нет cookie или сессия отозвана.</response>
    [HttpGet]
    [ProducesResponseType<MeHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await currentUserService.GetAsync(cancellationToken);

        return result.IsSuccess
            ? Ok(MeHttpResponse.From(result.Value))
            : result.Error.ToProblem(this);
    }

    /// <summary>
    /// Правка личных данных: ФИО и телефон. Должность меняется в организации —
    /// она принадлежит участию, а не пользователю.
    /// </summary>
    /// <response code="200">Профиль после изменений.</response>
    /// <response code="400">Некорректные поля.</response>
    /// <response code="401">Нет cookie или сессия отозвана.</response>
    [HttpPatch]
    [RequireAntiforgeryToken]
    [ProducesResponseType<MeHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(
        [FromBody] UpdateProfileHttpRequest request,
        CancellationToken cancellationToken)
    {
        var result = await currentUserService.UpdateAsync(
            new UpdateProfileRequest(request.FullName, request.Phone),
            cancellationToken);

        return result.IsSuccess
            ? Ok(MeHttpResponse.From(result.Value))
            : result.Error.ToProblem(this);
    }
}
