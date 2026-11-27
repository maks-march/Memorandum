using Memorandum.Domain.Common;

namespace Memorandum.Domain.Dictionaries;

/// <summary>Вид справочника: определяет экран и место использования.</summary>
public enum DictionaryKind
{
    /// <summary>Субъекты РФ: код региона в адресе объекта.</summary>
    Region = 1,

    /// <summary>ОКТМО: обязательные код и наименование с версии схемы 01.06.</summary>
    Oktmo = 2,

    /// <summary>Единицы измерения для таблицы ТЭП.</summary>
    Unit = 3,

    /// <summary>Каталог ТЭП: подсказки по типу объекта.</summary>
    TepCatalog = 4,
}

/// <summary>
/// Элемент справочника: регион, ОКТМО, единица измерения, показатель ТЭП.
/// </summary>
/// <remarks>
/// Read-only для API: наполняется загрузкой файлов (ОКТМО обновляется),
/// поэтому у сущности нет метода правки полей — только активность и версия набора.
/// </remarks>
public sealed class Dictionary : AuditableEntity
{
    public const int CodeMaxLength = 20;
    public const int NameMaxLength = 300;
    public const int ParentCodeMaxLength = 20;
    public const int VersionMaxLength = 20;

    private Dictionary()
    {
    }

    public DictionaryKind Kind { get; private set; }

    /// <summary>Код элемента: код региона, ОКТМО, обозначение единицы измерения.</summary>
    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    /// <summary>Вышестоящий элемент: регион для ОКТМО, объект для показателя ТЭП.</summary>
    public string? ParentCode { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>Версия набора, из которого загружен элемент: по ней видно устаревшие записи.</summary>
    public string Version { get; private set; } = null!;

    public int SortOrder { get; private set; }

    public static Dictionary Create(
        DictionaryKind kind,
        string? code,
        string? name,
        string? version,
        DateTime utcNow,
        string? parentCode = null,
        int sortOrder = 0,
        string? createdBy = null)
    {
        var item = new Dictionary
        {
            Kind = kind,
            Code = Guard.Required(code, "Код элемента справочника", CodeMaxLength),
            Name = Guard.Required(name, "Наименование элемента справочника", NameMaxLength),
            ParentCode = Guard.Optional(parentCode, "Вышестоящий код", ParentCodeMaxLength),
            Version = Guard.Required(version, "Версия справочника", VersionMaxLength),
            IsActive = true,
            SortOrder = sortOrder,
        };

        item.MarkCreated(utcNow, createdBy);
        return item;
    }

    public void Rename(string? name, DateTime utcNow, string? updatedBy = null)
    {
        Name = Guard.Required(name, "Наименование элемента справочника", NameMaxLength);
        Touch(utcNow, updatedBy);
    }

    /// <summary>Устаревшие элементы не удаляются: на них ссылаются уже проверенные документы.</summary>
    public void Deactivate(DateTime utcNow, string? updatedBy = null)
    {
        IsActive = false;
        Touch(utcNow, updatedBy);
    }

    public void Activate(DateTime utcNow, string? updatedBy = null)
    {
        IsActive = true;
        Touch(utcNow, updatedBy);
    }
}
