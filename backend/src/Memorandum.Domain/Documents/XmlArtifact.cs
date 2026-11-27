using Memorandum.Domain.Common;

namespace Memorandum.Domain.Documents;

/// <summary>Файл XML, собранный по проверенному документу.</summary>
/// <remarks>
/// Хранится хеш и размер, а не содержимое: XML воспроизводим из документа и проверки,
/// а хеш фиксирует, что именно ушло в экспертизу.
/// </remarks>
public sealed class XmlArtifact : AuditableEntity
{
    public const int Sha256Length = 64;
    public const int FileNameMaxLength = 255;

    private XmlArtifact()
    {
    }

    internal XmlArtifact(
        Document document,
        DocumentCheck check,
        string? sha256,
        long sizeBytes,
        DateTime utcNow,
        string? createdBy)
    {
        Document = document;
        DocumentId = document.Id;
        Check = check;
        CheckId = check.Id;
        Sha256 = Guard.Matches(sha256, "Хеш XML", Guard.Sha256(), "64 шестнадцатеричных символа", Sha256Length);
        SizeBytes = Guard.NotNegative(sizeBytes, "Размер XML");
        GeneratedAt = utcNow;

        MarkCreated(utcNow, createdBy);
    }

    public long DocumentId { get; private set; }

    public Document Document { get; private set; } = null!;

    /// <summary>Проверка, по результатам которой собран XML.</summary>
    public long CheckId { get; private set; }

    public DocumentCheck Check { get; private set; } = null!;

    public DateTime GeneratedAt { get; private set; }

    public string Sha256 { get; private set; } = null!;

    public long SizeBytes { get; private set; }

    /// <summary>Имя файла для скачивания; собирается из шифра и версии схемы.</summary>
    public string SuggestedFileName => $"{Document.Title}-{Check.SchemaVersionId}.xml";
}
