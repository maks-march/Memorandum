using Memorandum.Domain.Common;

namespace Memorandum.Domain.Schemas;

/// <summary>Тип поля формы: от него зависит виджет редактора.</summary>
public enum FieldKind
{
    String = 1,

    Text = 2,

    Number = 3,

    Date = 4,

    Boolean = 5,

    /// <summary>Выбор из справочника.</summary>
    Reference = 6,

    /// <summary>Таблица повторяющихся строк (ТЭП, состав документации).</summary>
    Table = 7,
}

/// <summary>
/// Поле формы: строка каталога, из которого строятся manifest, прогресс заполнения
/// и дифф версий. Не сущность: часть агрегата <see cref="SchemaVersion"/>.
/// </summary>
public sealed class SchemaField
{
    public const int FieldIdMaxLength = 100;
    public const int SectionMaxLength = 50;
    public const int LabelMaxLength = 300;
    public const int DictionaryMaxLength = 50;
    public const int FormatMaxLength = 50;
    public const int HintMaxLength = 500;
    public const int GroupMaxLength = 100;
    public const int XmlPathMaxLength = 500;

    private SchemaField()
    {
    }

    internal SchemaField(
        string? fieldId,
        string? section,
        string? label,
        FieldKind kind,
        bool required,
        int sortOrder,
        string? dictionary = null,
        string? format = null,
        string? hint = null,
        string? group = null,
        string? xmlPath = null)
    {
        FieldId = Guard.Required(fieldId, "Идентификатор поля", FieldIdMaxLength);
        Section = Guard.Required(section, "Раздел", SectionMaxLength);
        Label = Guard.Required(label, "Название поля", LabelMaxLength);
        Kind = kind;
        Required = required;
        SortOrder = sortOrder;
        Dictionary = Guard.Optional(dictionary, "Справочник", DictionaryMaxLength);
        Format = Guard.Optional(format, "Формат", FormatMaxLength);
        Hint = Guard.Optional(hint, "Подсказка", HintMaxLength);
        Group = Guard.Optional(group, "Группа", GroupMaxLength);
        XmlPath = Guard.Optional(xmlPath, "Путь в XML", XmlPathMaxLength);
    }

    /// <summary>Стабильный идентификатор поля: по нему сохраняются значения и строится дифф.</summary>
    public string FieldId { get; private set; } = null!;

    /// <summary>Код раздела документа, к которому относится поле.</summary>
    public string Section { get; private set; } = null!;

    public string Label { get; private set; } = null!;

    public FieldKind Kind { get; private set; }

    public bool Required { get; private set; }

    /// <summary>Справочник для выбора значения (regions, oktmo, units, tep-catalog).</summary>
    public string? Dictionary { get; private set; }

    public string? Format { get; private set; }

    public string? Hint { get; private set; }

    /// <summary>Группа полей внутри раздела: подзаголовок формы.</summary>
    public string? Group { get; private set; }

    /// <summary>Путь в XML, куда попадает значение: нужен для ошибок валидации.</summary>
    public string? XmlPath { get; private set; }

    public int SortOrder { get; private set; }
}
