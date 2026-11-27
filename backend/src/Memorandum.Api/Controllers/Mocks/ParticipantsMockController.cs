using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Memorandum.Api.Contracts.Participants;

namespace Memorandum.Api.Controllers.Mocks;

/// <summary>Заглушки участников документа.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/mocks/documents/{id:long}/participants")]
[Tags("Mocks")]
[AllowAnonymous]
public sealed class ParticipantsMockController : ControllerBase
{
    /// <summary>Участники документа.</summary>
    /// <response code="200">Заказчик, проектировщик и ГИП.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<DocumentParticipantHttpResponse>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<DocumentParticipantHttpResponse>> List(long id) =>
        Ok(DocumentParticipantHttpResponse.MockList());
}
