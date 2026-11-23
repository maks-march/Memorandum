using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Memorandum.Api.Contracts.Projects;

namespace Memorandum.Api.Controllers.Mocks;

/// <summary>Заглушки пояснительных записок: карточка и разделы документа.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/mocks/documents")]
[Tags("Mocks")]
[AllowAnonymous]
public sealed class DocumentsMockController : ControllerBase
{
    /// <summary>Карточка документа.</summary>
    /// <response code="200">Черновик пояснительной записки.</response>
    [HttpGet("{id:long}")]
    [ProducesResponseType<ProjectDocumentHttpResponse>(StatusCodes.Status200OK)]
    public ActionResult<ProjectDocumentHttpResponse> Get(long id) =>
        Ok(ProjectDocumentHttpResponse.Mock(id: id));

    /// <summary>Разделы документа.</summary>
    /// <response code="200">Три раздела: объект, ТЭП, состав документации.</response>
    [HttpGet("{id:long}/sections")]
    [ProducesResponseType<IReadOnlyList<DocumentSectionHttpResponse>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<DocumentSectionHttpResponse>> Sections(long id) =>
        Ok(DocumentSectionHttpResponse.MockList());
}
