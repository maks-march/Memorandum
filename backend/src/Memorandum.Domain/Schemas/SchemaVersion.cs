using Memorandum.Domain.Common;

namespace Memorandum.Domain.Schemas;

/// <summary>
/// Версия XML-схемы Минстроя (комплект XSD/XSL и каталог полей), по которой
/// заполняются документы. Агрегат: файлы, поля и публикация меняются через него.
/// </summary>
public sealed class SchemaVersion : AuditableEntity
{
    public const int CodeMaxLength = 20;
    public const int TitleMaxLength = 300;
    public const int Sha256Length = 64;

    private readonly List<SchemaFile> _files = [];
    private readonly List<SchemaField> _fields = [];

    private SchemaVersion()
    {
    }

    /// <summary>Код версии, он же идентификатор в API: «01.07».</summary>
    public string Code { get; private set; } = null!;

    public string Title { get; private set; } = null!;

    /// <summary>С какой даты документы можно заполнять по этой версии.</summary>
    public DateTime EffectiveFrom { get; private set; }

    public DateTime? PublishedAt { get; private set; }

    /// <summary>Выключенная версия не предлагается для новых документов, старые открываются.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Версия по умолчанию: подставляется в форму создания документа.</summary>
    public bool IsDefault { get; private set; }

    /// <summary>Хеш загруженного комплекта: по нему видно, что файлы не подменили.</summary>
    public string? Sha256 { get; private set; }

    public IReadOnlyCollection<SchemaFile> Files => _files;

    public IReadOnlyCollection<SchemaField> Fields => _fields;

    public static SchemaVersion Register(
        string? code,
        string? title,
        DateTime effectiveFrom,
        DateTime utcNow,
        string? createdBy = null)
    {
        var version = new SchemaVersion
        {
            Code = Guard.Required(code, "Код версии", CodeMaxLength),
            Title = Guard.Required(title, "Название версии", TitleMaxLength),
            EffectiveFrom = effectiveFrom,
            IsActive = true,
            IsDefault = false,
        };

        version.MarkCreated(utcNow, createdBy);
        return version;
    }

    public void Update(
        string? title,
        DateTime effectiveFrom,
        DateTime utcNow,
        string? updatedBy = null)
    {
        Title = Guard.Required(title, "Название версии", TitleMaxLength);
        EffectiveFrom = effectiveFrom;
        Touch(utcNow, updatedBy);
    }

    /// <summary>
    /// Публикация комплекта: файлы и каталог полей приходят вместе, поэтому
    /// собираются здесь, а не по одному.
    /// </summary>
    public void Publish(
        string? sha256,
        IReadOnlyCollection<SchemaFile> files,
        IReadOnlyCollection<SchemaField> fields,
        DateTime utcNow,
        string? updatedBy = null)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(fields);

        if (files.Count == 0)
            throw new ValidationException(ErrorCodes.Validation, "В комплекте схемы нет файлов.");

        Sha256 = Guard.Required(sha256, "Хеш комплекта", Sha256Length);

        _files.Clear();
        _files.AddRange(files);

        _fields.Clear();
        _fields.AddRange(fields);

        PublishedAt ??= utcNow;
        Touch(utcNow, updatedBy);
    }

    public void Activate(DateTime utcNow, string? updatedBy = null)
    {
        IsActive = true;
        Touch(utcNow, updatedBy);
    }

    public void Deactivate(DateTime utcNow, string? updatedBy = null)
    {
        IsActive = false;

        // Версия по умолчанию обязана быть активной: иначе форма создания
        // документа предложит схему, которой нет.
        IsDefault = false;

        Touch(utcNow, updatedBy);
    }

    /// <summary>Версия по умолчанию одна на всю систему — прежнюю снимает вызывающий сценарий.</summary>
    public void MarkDefault(DateTime utcNow, string? updatedBy = null)
    {
        if (!IsActive)
            throw new ConflictException(
                ErrorCodes.SchemaVersionInactive,
                "Версия по умолчанию должна быть активной.");

        IsDefault = true;
        Touch(utcNow, updatedBy);
    }

    public void UnmarkDefault(DateTime utcNow, string? updatedBy = null)
    {
        if (!IsDefault)
            return;

        IsDefault = false;
        Touch(utcNow, updatedBy);
    }

    /// <summary>Версия используется документом: тогда её нельзя удалить, только выключить.</summary>
    public void EnsureUnused(int documentsCount)
    {
        if (documentsCount > 0)
            throw new ConflictException(
                ErrorCodes.ReferenceInUse,
                $"По версии {Code} заполняется документов: {documentsCount}. Версию можно только выключить.");
    }
}
