namespace Memorandum.Api.Contracts.Schemas;

/// <summary>Поле формы из каталога версии схемы.</summary>
/// <param name="FieldId">Идентификатор поля внутри раздела.</param>
/// <param name="Section">Код раздела: object, tep, composition и т. д.</param>
/// <param name="Label">Подпись поля.</param>
/// <param name="Kind">Виджет: String, Text, Number, Date, Boolean, Reference, Table.</param>
/// <param name="Required">Поле обязательное для проверки.</param>
/// <param name="Dictionary">Справочник для Reference и Table.</param>
/// <param name="XmlPath">Куда значение попадает в XML.</param>
public sealed record SchemaFieldHttpResponse(
    string FieldId,
    string Section,
    string Label,
    string Kind,
    bool Required,
    string? Dictionary,
    string? XmlPath)
{
    /// <summary>Моковый каталог полей.</summary>
    public static SchemaFieldHttpResponse Mock(string fieldId = "objectName", string section = "object") =>
        new(
            fieldId,
            section,
            "Наименование объекта",
            "String",
            Required: true,
            Dictionary: null,
            "/Документ/Объект/Наименование");

    /// <summary>Моковый список полей трёх разделов.</summary>
    public static IReadOnlyList<SchemaFieldHttpResponse> MockList() =>
        [
            Mock(),
            Mock("oktmoCode", "object") with { Label = "Код ОКТМО", Dictionary = "oktmo" },
            Mock("rows", "tep") with { Label = "Показатели", Kind = "Table", Dictionary = "tep-catalog" },
            Mock("documents", "composition") with { Label = "Состав документации", Kind = "Table" },
        ];
}
