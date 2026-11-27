using System.Text.Json;
using Memorandum.Domain.Common;

namespace Memorandum.Domain.Documents;

/// <summary>Импорт XML в документ: исходный файл и итог переноса.</summary>
/// <remarks>
/// Неудачная и частичная попытка тоже сохраняется: пользователь должен видеть,
/// какой файл он загружал и что не перенеслось.
/// </remarks>
public sealed class DocumentImport : AuditableEntity
{
    public const int FileNameMaxLength = 255;
    public const int SchemaCodeMaxLength = 20;
    public const int WarningMaxLength = 500;

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private DocumentImport()
    {
    }

    internal DocumentImport(
        Document document,
        string? fileName,
        string? detectedSchemaCode,
        ImportStatus status,
        int importedFields,
        IReadOnlyCollection<string> warnings,
        byte[]? originalFile,
        DateTime utcNow,
        string? createdBy)
    {
        Document = document;
        DocumentId = document.Id;
        FileName = Guard.Optional(fileName, "Имя файла", FileNameMaxLength);
        DetectedSchemaCode = Guard.Optional(detectedSchemaCode, "Версия схемы XML", SchemaCodeMaxLength);
        Status = status;
        ImportedFields = Guard.NotNegative(importedFields, "Количество полей");
        WarningsJson = JsonSerializer.Serialize(warnings, Options);
        OriginalFile = originalFile;
        ImportedAt = utcNow;

        MarkCreated(utcNow, createdBy);
    }

    public long DocumentId { get; private set; }

    public Document Document { get; private set; } = null!;

    public string? FileName { get; private set; }

    /// <summary>Версия схемы, определённая по самому XML.</summary>
    public string? DetectedSchemaCode { get; private set; }

    public ImportStatus Status { get; private set; }

    public int ImportedFields { get; private set; }

    /// <summary>Что не удалось перенести: JSON-массив сообщений.</summary>
    public string WarningsJson { get; private set; } = "[]";

    /// <summary>Исходный файл: нужен для повторного разбора спорных случаев.</summary>
    public byte[]? OriginalFile { get; private set; }

    public DateTime ImportedAt { get; private set; }

    public IReadOnlyCollection<string> Warnings =>
        JsonSerializer.Deserialize<string[]>(WarningsJson, Options) ?? [];
}
