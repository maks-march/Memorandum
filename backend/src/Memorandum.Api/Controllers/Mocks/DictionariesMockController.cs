using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Memorandum.Api.Contracts.Dictionaries;

namespace Memorandum.Api.Controllers.Mocks;

/// <summary>Заглушки справочников: регионы, ОКТМО, единицы измерения, каталог ТЭП.</summary>
/// <remarks>Только чтение: справочники наполняются загрузкой файлов.</remarks>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/mocks/dictionaries")]
[Tags("Mocks")]
[AllowAnonymous]
public sealed class DictionariesMockController : ControllerBase
{
    /// <summary>Субъекты РФ.</summary>
    /// <response code="200">Три региона.</response>
    [HttpGet("regions")]
    [ProducesResponseType<IReadOnlyList<DictionaryItemHttpResponse>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<DictionaryItemHttpResponse>> Regions() =>
        Ok(DictionaryItemHttpResponse.MockRegions());

    /// <summary>ОКТМО с фильтром по региону.</summary>
    /// <param name="q">Поиск по коду или наименованию.</param>
    /// <param name="region">Код региона.</param>
    /// <response code="200">Два муниципальных образования Свердловской области.</response>
    [HttpGet("oktmo")]
    [ProducesResponseType<IReadOnlyList<DictionaryItemHttpResponse>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<DictionaryItemHttpResponse>> Oktmo(string? q, string? region) =>
        Ok(DictionaryItemHttpResponse.MockOktmo());

    /// <summary>Единицы измерения для ТЭП.</summary>
    /// <response code="200">м², м³, м, шт., этажи.</response>
    [HttpGet("units")]
    [ProducesResponseType<IReadOnlyList<DictionaryItemHttpResponse>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<DictionaryItemHttpResponse>> Units() =>
        Ok(DictionaryItemHttpResponse.MockUnits());

    /// <summary>Каталог ТЭП по типу объекта.</summary>
    /// <param name="objectKind">Тип объекта: Linear, Industrial, NonIndustrial.</param>
    /// <response code="200">Три показателя.</response>
    [HttpGet("tep-catalog")]
    [ProducesResponseType<IReadOnlyList<DictionaryItemHttpResponse>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<DictionaryItemHttpResponse>> TepCatalog(string? objectKind) =>
        Ok(DictionaryItemHttpResponse.MockTepCatalog());
}
