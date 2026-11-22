using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Memorandum.Api.Contracts;
using Memorandum.Api.Mapping;
using Memorandum.Application.CurrentUser;

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
}
