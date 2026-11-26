using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Memorandum.Api.Contracts.Schemas;

namespace Memorandum.Api.Controllers.Mocks;

/// <summary>Заглушки версий XML-схем.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/mocks/schema-versions")]
[Tags("Mocks")]
[AllowAnonymous]
public sealed class SchemasMockController : ControllerBase
{
    /// <summary>Активные версии схем.</summary>
    /// <response code="200">Две версии, 01.07 — по умолчанию.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<SchemaVersionHttpResponse>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<SchemaVersionHttpResponse>> List() =>
        Ok(SchemaVersionHttpResponse.MockList());

    /// <summary>Каталог полей версии.</summary>
    /// <response code="200">Поля трёх разделов.</response>
    [HttpGet("{code}/fields")]
    [ProducesResponseType<IReadOnlyList<SchemaFieldHttpResponse>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<SchemaFieldHttpResponse>> Fields(string code) =>
        Ok(SchemaFieldHttpResponse.MockList());
}
