using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Memorandum.Api.Contracts.Projects;

namespace Memorandum.Api.Controllers.Mocks;

/// <summary>
/// Заглушки проектов: фронт работает без БД. Ответы те же, что у реальных
/// эндпоинтов, — DTO не меняется, меняется только источник данных.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/mocks/projects")]
[Tags("Mocks")]
[AllowAnonymous]
public sealed class ProjectsMockController : ControllerBase
{
    /// <summary>Список проектов организации.</summary>
    /// <response code="200">Два проекта, один в архиве.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ProjectHttpResponse>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<ProjectHttpResponse>> List() =>
        Ok(ProjectHttpResponse.MockList());

    /// <summary>Карточка проекта.</summary>
    /// <response code="200">Проект с тестовыми данными.</response>
    [HttpGet("{id:long}")]
    [ProducesResponseType<ProjectHttpResponse>(StatusCodes.Status200OK)]
    public ActionResult<ProjectHttpResponse> Get(long id) =>
        Ok(ProjectHttpResponse.Mock(id));

    /// <summary>Документы проекта.</summary>
    /// <response code="200">Две редакции пояснительной записки.</response>
    [HttpGet("{id:long}/documents")]
    [ProducesResponseType<IReadOnlyList<ProjectDocumentHttpResponse>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<ProjectDocumentHttpResponse>> Documents(long id) =>
        Ok(ProjectDocumentHttpResponse.MockList(id));
}
