using Memorandum.Domain.Common;

namespace Memorandum.Domain.Schemas;

/// <summary>
/// Файл комплекта схемы с содержимым: XSD, XSL или приложение.
/// </summary>
/// <remarks>
/// Комплект грузится целиком: `XmlSchemaSet` компилируется только по полному набору
/// файлов, поэтому содержимое хранится в БД, а не ссылкой на хранилище.
/// </remarks>
public sealed class SchemaPackageFile : AuditableEntity
{
    public const int PathMaxLength = 300;
    public const int ContentTypeMaxLength = 100;

    private SchemaPackageFile()
    {
    }

    public SchemaPackageFile(
        SchemaFileKind kind,
        string? path,
        string? contentType,
        byte[] content,
        DateTime utcNow,
        string? createdBy = null)
    {
        ArgumentNullException.ThrowIfNull(content);

        Kind = kind;
        Path = Guard.Required(path, "Путь в комплекте", PathMaxLength);
        ContentType = Guard.Optional(contentType, "Тип содержимого", ContentTypeMaxLength) ?? "application/xml";
        Content = content;
        Sha256 = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(content));

        MarkCreated(utcNow, createdBy);
    }

    public long SchemaVersionId { get; private set; }

    public SchemaVersion SchemaVersion { get; private set; } = null!;

    public SchemaFileKind Kind { get; private set; }

    /// <summary>Путь внутри комплекта: совпадает с <c>schemaLocation</c> в XSD.</summary>
    public string Path { get; private set; } = null!;

    public string ContentType { get; private set; } = null!;

    /// <summary>Содержимое файла: неизменяемо после публикации версии.</summary>
    public byte[] Content { get; private set; } = null!;

    public string Sha256 { get; private set; } = null!;

    public long SizeBytes => Content.LongLength;
}
