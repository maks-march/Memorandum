using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Memorandum.Api.Contracts;
using Memorandum.Api.Contracts.Documents;
using Memorandum.Api.Contracts.Projects;

namespace Memorandum.Api.Controllers.Mocks;

/// <summary>Заглушки проектов.</summary>
/// <remarks>
/// Только чтение: фронт получает ответы той же формы, что у реальных эндпоинтов,
/// и работает без БД. Запросы, меняющие данные, здесь не повторяются.
/// </remarks>
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
    [ProducesResponseType<PagedHttpResponse<ProjectHttpResponse>>(StatusCodes.Status200OK)]
    public ActionResult<PagedHttpResponse<ProjectHttpResponse>> List() =>
        Ok(PagedHttpResponse<ProjectHttpResponse>.Mock(ProjectHttpResponse.MockList()));

    /// <summary>Карточка проекта.</summary>
    /// <response code="200">Проект и счётчики документов.</response>
    [HttpGet("{id:long}")]
    [ProducesResponseType<ProjectDetailsHttpResponse>(StatusCodes.Status200OK)]
    public ActionResult<ProjectDetailsHttpResponse> Get(long id) =>
        Ok(ProjectDetailsHttpResponse.Mock(id));

    /// <summary>Документы проекта.</summary>
    /// <response code="200">Четыре документа в разных статусах.</response>
    [HttpGet("{id:long}/documents")]
    [ProducesResponseType<PagedHttpResponse<DocumentHttpResponse>>(StatusCodes.Status200OK)]
    public ActionResult<PagedHttpResponse<DocumentHttpResponse>> Documents(long id) =>
        Ok(PagedHttpResponse<DocumentHttpResponse>.Mock(DocumentHttpResponse.MockList()));
}
