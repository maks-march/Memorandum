
namespace Memorandum.Api.Contracts.Schemas;

/// <summary>Поле формы по каталогу схемы.</summary>
/// <param name="FieldId">Стабильный идентификатор поля.</param>
/// <param name="Section">Код раздела документа.</param>
/// <param name="Label">Название поля.</param>
/// <param name="Kind">Тип поля: String, Number, Date, Table и т. д.</param>
/// <param name="Required">Обязательное поле.</param>
/// <param name="Group">Группа внутри раздела.</param>
/// <param name="Hint">Подсказка.</param>
public sealed record SchemaFieldHttpResponse(
    string FieldId,
    string Section,
    string Label,
    string Kind,
    bool Required,
    string? Group,
    string? Hint)
{
    /// <summary>Моковый каталог полей для <c>/api/v1/mocks/schema-versions/{code}/fields</c>.</summary>
    public static IReadOnlyList<SchemaFieldHttpResponse> MockList() =>
        [
            new("object-info.name", "object-info", "Наименование объекта", "String", Required: true, "Объект", null),
            new("object-info.area", "object-info", "Площадь, м²", "Number", Required: true, "Объект", null),
            new("object-info.address", "object-info", "Адрес", "String", Required: false, "Объект", null),
            new("tep.rows", "tep", "Технико-экономические показатели", "Table", Required: true, "ТЭП", "Строки таблицы"),
            new("composition.rows", "composition", "Состав документации", "Table", Required: true, "Состав", null),
        ];
}
