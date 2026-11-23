using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Memorandum.Api.Contracts.Organizations;

namespace Memorandum.Api.Controllers.Mocks;

/// <summary>Заглушки приглашений сотрудников в организацию.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/mocks/invitations")]
[Tags("Mocks")]
[AllowAnonymous]
public sealed class InvitationsMockController : ControllerBase
{
    /// <summary>Список ожидающих приглашений.</summary>
    /// <response code="200">Два приглашения.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<InvitationHttpResponse>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<InvitationHttpResponse>> List() =>
        Ok(InvitationHttpResponse.MockList());
}
