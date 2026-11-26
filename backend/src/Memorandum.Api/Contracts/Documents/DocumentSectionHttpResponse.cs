
namespace Memorandum.Api.Contracts.Documents;

/// <summary>Раздел пояснительной записки: раздел редактора.</summary>
/// <param name="Id">Идентификатор раздела.</param>
/// <param name="Code">Код раздела по схеме.</param>
/// <param name="Title">Название раздела.</param>
/// <param name="SortOrder">Порядок в документе.</param>
/// <param name="DataJson">Содержимое раздела, JSON схемы.</param>
public sealed record DocumentSectionHttpResponse(
    long Id,
    string Code,
    string Title,
    int SortOrder,
    string? DataJson)
{
    /// <summary>Моковая версия для разделов документа.</summary>
    public static DocumentSectionHttpResponse Mock(long id = 1, int sortOrder = 1) =>
        new(
            id,
            "object-info",
            "Сведения об объекте капитального строительства",
            sortOrder,
            """{"objectName":"Многоквартирный жилой дом","address":"г. Екатеринбург, ул. Ленина, 15"}""");

    /// <summary>Моковый список: три раздела.</summary>
    public static IReadOnlyList<DocumentSectionHttpResponse> MockList() =>
        [
            Mock(1, 1),
            Mock(2, 2) with { Code = "tep", Title = "Технико-экономические показатели" },
            Mock(3, 3) with { Code = "composition", Title = "Состав проектной документации" },
        ];
}
