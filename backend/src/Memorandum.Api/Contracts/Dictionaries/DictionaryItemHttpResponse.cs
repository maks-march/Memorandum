namespace Memorandum.Api.Contracts.Dictionaries;

/// <summary>Элемент справочника: регион, ОКТМО, единица измерения, показатель ТЭП.</summary>
/// <param name="Code">Код элемента.</param>
/// <param name="Name">Наименование.</param>
/// <param name="ParentCode">Вышестоящий элемент: регион для ОКТМО, объект для ТЭП.</param>
public sealed record DictionaryItemHttpResponse(string Code, string Name, string? ParentCode)
{
    /// <summary>Моковый список для <c>/api/v1/mocks/dictionaries/regions</c>.</summary>
    public static IReadOnlyList<DictionaryItemHttpResponse> MockRegions() =>
        [
            new("66", "Свердловская область", ParentCode: null),
            new("77", "Москва", ParentCode: null),
            new("78", "Санкт-Петербург", ParentCode: null),
        ];

    /// <summary>Моковый список для <c>/api/v1/mocks/dictionaries/oktmo</c>.</summary>
    public static IReadOnlyList<DictionaryItemHttpResponse> MockOktmo() =>
        [
            new("65701000", "город Екатеринбург", "66"),
            new("65765000", "город Нижний Тагил", "66"),
        ];

    /// <summary>Моковый список для <c>/api/v1/mocks/dictionaries/units</c>.</summary>
    public static IReadOnlyList<DictionaryItemHttpResponse> MockUnits() =>
        [
            new("m2", "м²", ParentCode: null),
            new("m3", "м³", ParentCode: null),
            new("m", "м", ParentCode: null),
            new("pcs", "шт.", ParentCode: null),
            new("floor", "этажи", ParentCode: null),
        ];

    /// <summary>Моковый список для <c>/api/v1/mocks/dictionaries/tep-catalog</c>.</summary>
    public static IReadOnlyList<DictionaryItemHttpResponse> MockTepCatalog() =>
        [
            new("area_total", "Общая площадь", "NonIndustrial"),
            new("area_build", "Площадь застройки", "NonIndustrial"),
            new("length", "Протяжённость", "Linear"),
        ];
}
