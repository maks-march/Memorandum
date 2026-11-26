using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Memorandum.Api.Contracts;
using Memorandum.Api.Contracts.Documents;

namespace Memorandum.Api.Controllers.Mocks;

/// <summary>Заглушки пояснительных записок.</summary>
/// <remarks>Только чтение: списки, карточка, разделы, версии, импорты и проверки.</remarks>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/mocks/documents")]
[Tags("Mocks")]
[AllowAnonymous]
public sealed class DocumentsMockController : ControllerBase
{
    /// <summary>Список документов.</summary>
    /// <response code="200">Четыре документа в разных статусах.</response>
    [HttpGet]
    [ProducesResponseType<PagedHttpResponse<DocumentHttpResponse>>(StatusCodes.Status200OK)]
    public ActionResult<PagedHttpResponse<DocumentHttpResponse>> List() =>
        Ok(PagedHttpResponse<DocumentHttpResponse>.Mock(DocumentHttpResponse.MockList()));

    /// <summary>Карточка документа.</summary>
    /// <response code="200">Документ, разделы и прогресс заполнения.</response>
    [HttpGet("{id:long}")]
    [ProducesResponseType<DocumentDetailsHttpResponse>(StatusCodes.Status200OK)]
    public ActionResult<DocumentDetailsHttpResponse> Get(long id) =>
        Ok(DocumentDetailsHttpResponse.Mock(id));

    /// <summary>Разделы документа.</summary>
    /// <response code="200">Три раздела: объект, ТЭП, состав документации.</response>
    [HttpGet("{id:long}/sections")]
    [ProducesResponseType<IReadOnlyList<DocumentSectionHttpResponse>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<DocumentSectionHttpResponse>> Sections(long id) =>
        Ok(DocumentSectionHttpResponse.MockList());

    /// <summary>Прогресс заполнения.</summary>
    /// <response code="200">Проценты и разбивка по разделам.</response>
    [HttpGet("{id:long}/completion")]
    [ProducesResponseType<DocumentCompletionHttpResponse>(StatusCodes.Status200OK)]
    public ActionResult<DocumentCompletionHttpResponse> Completion(long id) =>
        Ok(DocumentCompletionHttpResponse.Mock());

    /// <summary>История версий.</summary>
    /// <response code="200">Две версии: ручная и автоматическая.</response>
    [HttpGet("{id:long}/versions")]
    [ProducesResponseType<IReadOnlyList<DocumentVersionHttpResponse>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<DocumentVersionHttpResponse>> Versions(long id) =>
        Ok(DocumentVersionHttpResponse.MockList());

    /// <summary>История импортов.</summary>
    /// <response code="200">Один импорт с предупреждением.</response>
    [HttpGet("{id:long}/imports")]
    [ProducesResponseType<IReadOnlyList<DocumentImportHttpResponse>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<DocumentImportHttpResponse>> Imports(long id) =>
        Ok(DocumentImportHttpResponse.MockList());

    /// <summary>История проверок.</summary>
    /// <response code="200">Последняя проверка: ошибка и предупреждение.</response>
    [HttpGet("{id:long}/checks")]
    [ProducesResponseType<IReadOnlyList<DocumentCheckHttpResponse>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<DocumentCheckHttpResponse>> Checks(long id) =>
        Ok(DocumentCheckHttpResponse.MockList());
}
