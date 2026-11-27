using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Memorandum.Api.Contracts;
using Memorandum.Api.Contracts.Admin;

namespace Memorandum.Api.Controllers.Mocks;

/// <summary>Заглушки администрирования: аудит и журнал ошибок.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/mocks/admin")]
[Tags("Mocks")]
[AllowAnonymous]
public sealed class AdminMockController : ControllerBase
{
    /// <summary>Аудит действий.</summary>
    /// <response code="200">Три последних события.</response>
    [HttpGet("audit")]
    [ProducesResponseType<PagedHttpResponse<AuditEntryHttpResponse>>(StatusCodes.Status200OK)]
    public ActionResult<PagedHttpResponse<AuditEntryHttpResponse>> Audit() =>
        Ok(PagedHttpResponse<AuditEntryHttpResponse>.Mock(AuditEntryHttpResponse.MockList()));

    /// <summary>Журнал ошибок.</summary>
    /// <response code="200">Две ошибки, одна повторяется.</response>
    [HttpGet("errors")]
    [ProducesResponseType<PagedHttpResponse<AppErrorHttpResponse>>(StatusCodes.Status200OK)]
    public ActionResult<PagedHttpResponse<AppErrorHttpResponse>> Errors() =>
        Ok(PagedHttpResponse<AppErrorHttpResponse>.Mock(AppErrorHttpResponse.MockList()));
}
