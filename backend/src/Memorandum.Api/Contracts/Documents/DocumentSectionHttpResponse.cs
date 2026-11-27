namespace Memorandum.Api.Contracts.Documents;

/// <summary>Раздел пояснительной записки: раздел редактора.</summary>
/// <param name="Code">Код раздела: object, participants, specialists, basis, tep, composition, normatives, surveys, climate, extra.</param>
/// <param name="Title">Название раздела.</param>
/// <param name="Filled">Раздел заполнен.</param>
/// <param name="DataJson">Содержимое раздела, JSON каталога полей схемы.</param>
public sealed record DocumentSectionHttpResponse(
    string Code,
    string Title,
    bool Filled,
    string? DataJson)
{
    /// <summary>Моковая версия раздела.</summary>
    public static DocumentSectionHttpResponse Mock(string code = "object", string? title = null) =>
        new(
            code,
            title ?? "Сведения об объекте капитального строительства",
            Filled: true,
            """{"objectName":"Многоквартирный жилой дом","address":"г. Екатеринбург, ул. Ленина, 15","oktmoCode":"65701000"}""");

    /// <summary>Моковый список: три раздела.</summary>
    public static IReadOnlyList<DocumentSectionHttpResponse> MockList() =>
        [
            Mock("object"),
            Mock("tep", "Технико-экономические показатели"),
            Mock("composition", "Состав проектной документации"),
        ];
}
