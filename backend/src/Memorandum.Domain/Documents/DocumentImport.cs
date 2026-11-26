using Memorandum.Domain.Common;

namespace Memorandum.Domain.Documents;

/// <summary>
/// Импорт документа из XML: что распознано, что перенесено и что пришлось пропустить.
/// Исходный файл сохраняется отдельно, чтобы пользователь мог скачать его «как есть».
/// </summary>
public sealed class DocumentImport : AuditableEntity
{
    public const int SchemaCodeMaxLength = 20;
    public const int FileNameMaxLength = 300;
    public const int DetailsJsonMaxLength = 20_000;
    public const int StorageKeyMaxLength = 300;
    public const int Sha256Length = 64;

    private DocumentImport()
    {
    }

    public long DocumentId { get; private set; }

    public Document Document { get; private set; } = null!;

    /// <summary>Распознанная версия схемы; <c>null</c>, если схема не определена.</summary>
    public string? DetectedSchemaCode { get; private set; }

    /// <summary>Исходный файл в хранилище: ключ, размер и хеш. Скачивается «как есть».</summary>
    public string? OriginalFileKey { get; private set; }

    public long? OriginalFileSizeBytes { get; private set; }

    public string? OriginalFileSha256 { get; private set; }

    /// <summary>Имя загруженного файла: нужно, чтобы показать, откуда пришли данные.</summary>
    public string FileName { get; private set; } = null!;

    /// <summary>Сколько полей удалось перенести.</summary>
    public int ImportedFields { get; private set; }

    /// <summary>Предупреждения импорта: путь, причина. Хранятся как JSON-массив.</summary>
    public string? WarningsJson { get; private set; }

    internal DocumentImport(
        string? fileName,
        string? detectedSchemaCode,
        int importedFields,
        string? warningsJson,
        DateTime utcNow,
        string? createdBy = null,
        string? originalFileKey = null,
        long? originalFileSizeBytes = null,
        string? originalFileSha256 = null)
    {
        if (importedFields < 0)
            throw new ValidationException(ErrorCodes.Validation, "Число перенесённых полей не может быть отрицательным.");

        FileName = Guard.Required(fileName, "Имя файла", FileNameMaxLength);
        DetectedSchemaCode = Guard.Optional(detectedSchemaCode, "Код схемы", SchemaCodeMaxLength);
        ImportedFields = importedFields;
        WarningsJson = Guard.Optional(warningsJson, "Предупреждения", DetailsJsonMaxLength);
        OriginalFileKey = Guard.Optional(originalFileKey, "Ключ хранилища", StorageKeyMaxLength);
        OriginalFileSha256 = Guard.Optional(originalFileSha256, "Хеш файла", Sha256Length);

        if (originalFileSizeBytes is < 0)
            throw new ValidationException(ErrorCodes.Validation, "Размер файла не может быть отрицательным.");

        OriginalFileSizeBytes = originalFileSizeBytes;

        MarkCreated(utcNow, createdBy);
    }
}
