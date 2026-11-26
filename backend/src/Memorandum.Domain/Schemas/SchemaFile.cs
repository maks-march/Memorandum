using Memorandum.Domain.Common;

namespace Memorandum.Domain.Schemas;

/// <summary>Назначение файла в комплекте схемы.</summary>
public enum SchemaFileKind
{
    /// <summary>XSD: по нему валидируется сгенерированный XML.</summary>
    Xsd = 1,

    /// <summary>XSL: по нему строится HTML-предпросмотр.</summary>
    Xsl = 2,

    /// <summary>Вспомогательный файл комплекта (справочники, приложения).</summary>
    Other = 3,
}

/// <summary>
/// Файл комплекта схемы: XSD, XSL или приложение. Не сущность с ключом и аудитом —
/// часть агрегата <see cref="SchemaVersion"/>, поэтому значение-объект.
/// </summary>
public sealed class SchemaFile
{
    public const int PathMaxLength = 300;
    public const int NameMaxLength = 200;

    private SchemaFile()
    {
    }

    internal SchemaFile(
        SchemaFileKind kind,
        string? path,
        string? name,
        long sizeBytes,
        string? sha256,
        string? storedKey)
    {
        if (sizeBytes < 0)
            throw new ValidationException(ErrorCodes.Validation, "Размер файла не может быть отрицательным.");

        Kind = kind;
        Path = Guard.Required(path, "Путь в комплекте", PathMaxLength);
        Name = Guard.Required(name, "Имя файла", NameMaxLength);
        SizeBytes = sizeBytes;
        Sha256 = Guard.Required(sha256, "Хеш файла", SchemaVersion.Sha256Length);
        StoredKey = Guard.Optional(storedKey, "Ключ хранилища", PathMaxLength);
    }



    public SchemaFileKind Kind { get; private set; }

    /// <summary>Путь внутри архива комплекта: по нему видно структуру поставки.</summary>
    public string Path { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public long SizeBytes { get; private set; }

    public string Sha256 { get; private set; } = null!;

    /// <summary>Ключ в хранилище файлов; <c>null</c>, пока файл не сохранён.</summary>
    public string? StoredKey { get; private set; }
}
