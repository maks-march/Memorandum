using Memorandum.Domain.Common;
using Memorandum.Domain.Projects;

namespace Memorandum.Domain.Documents;

/// <summary>
/// Пояснительная записка в рамках проекта: основная сущность сервиса.
/// Агрегат: разделы, версии, импорты и проверки меняются только через него.
/// </summary>
public sealed class Document : AuditableEntity
{
    public const int TitleMaxLength = 500;
    public const int SchemaCodeMaxLength = 20;
    public const int CheckDetailsMaxLength = 20_000;
    public const int XmlSha256Length = 64;

    private readonly List<DocumentSection> _sections = [];
    private readonly List<DocumentVersion> _versions = [];
    private readonly List<DocumentImport> _imports = [];

    private Document()
    {
    }

    public long ProjectId { get; private set; }

    public Project Project { get; private set; } = null!;

    public string Title { get; private set; } = null!;

    /// <summary>
    /// Код XML-схемы Минстроя, по которой заполняется документ (например «01.07»).
    /// Задаётся при создании: смена схемы — это другой документ, а не правка.
    /// </summary>
    public string SchemaCode { get; private set; } = null!;

    public DocumentStatus Status { get; private set; }

    public bool IsArchived { get; private set; }

    /// <summary>Ответственный за документ; <c>null</c> — отвечает ответственный за проект.</summary>
    public long? ResponsibleUserId { get; private set; }

    public IReadOnlyCollection<DocumentSection> Sections => _sections;

    public IReadOnlyCollection<DocumentVersion> Versions => _versions;

    public IReadOnlyCollection<DocumentImport> Imports => _imports;

    /// <summary>Автор последней правки: колонка «Автор» в списке документов.</summary>
    public string? AuthorName => UpdatedBy;

    /// <summary>Когда документ проверяли последний раз; <c>null</c> — не проверялся.</summary>
    public DateTime? CheckedAt { get; private set; }

    /// <summary>Ошибок в последней проверке.</summary>
    public int CheckErrorCount { get; private set; }

    /// <summary>Предупреждений в последней проверке.</summary>
    public int CheckWarningCount { get; private set; }

    /// <summary>Замечания последней проверки: JSON-массив, как пришёл от проверки.</summary>
    public string? CheckErrorsJson { get; private set; }

    /// <summary>Хеш последнего сгенерированного XML; <c>null</c> — XML не генерировали.</summary>
    public string? XmlSha256 { get; private set; }

    public long? XmlSizeBytes { get; private set; }

    /// <summary>XML последней генерации прошёл XSD-валидацию.</summary>
    public bool? XmlValid { get; private set; }

    public static Document Create(
        Project project,
        string? title,
        string? schemaCode,
        DateTime utcNow,
        string? createdBy = null)
    {
        ArgumentNullException.ThrowIfNull(project);

        if (project.IsArchived)
            throw new ConflictException(
                ErrorCodes.ProjectArchived,
                "Нельзя создать документ в архивном проекте.");

        var document = new Document
        {
            Project = project,
            ProjectId = project.Id,
            Title = Guard.Required(title, "Заголовок документа", TitleMaxLength),
            SchemaCode = Guard.Required(schemaCode, "Код схемы", SchemaCodeMaxLength),
            Status = DocumentStatus.Draft,
        };

        document.MarkCreated(utcNow, createdBy);
        return document;
    }

    public void Rename(string? title, DateTime utcNow, string? updatedBy = null)
    {
        EnsureEditable("Нельзя изменить архивный документ.");

        Title = Guard.Required(title, "Заголовок документа", TitleMaxLength);
        Touch(utcNow, updatedBy);
    }

    /// <summary>Ответственным можно назначить только участника проекта: иначе он не увидит документ.</summary>
    public void AssignResponsible(long? userId, DateTime utcNow, string? updatedBy = null)
    {
        EnsureEditable("Нельзя изменить архивный документ.");

        if (userId is not null && !Project.HasMember(userId.Value))
            throw new ValidationException(
                ErrorCodes.ProjectMemberRequired,
                "Ответственным можно назначить только участника проекта.");

        ResponsibleUserId = userId;
        Touch(utcNow, updatedBy);
    }

    /// <summary>Раздел добавляется один раз на код: содержимое дальше меняется через <see cref="FillSection"/>.</summary>
    public DocumentSection AddSection(
        string? code,
        string? title,
        int sortOrder,
        DateTime utcNow,
        string? createdBy = null)
    {
        EnsureContentEditable("Нельзя изменить архивный документ.");

        var normalizedCode = Guard.Required(code, "Код раздела", DocumentSection.CodeMaxLength);

        if (FindSection(normalizedCode) is not null)
            throw new ConflictException(ErrorCodes.SectionDuplicate, $"Раздел «{normalizedCode}» уже есть в документе.");

        var section = new DocumentSection(this, normalizedCode, title, sortOrder, utcNow, createdBy);
        _sections.Add(section);

        // Новый раздел — это изменение содержимого: прежняя проверка больше не актуальна.
        Reopen();

        return section;
    }

    public DocumentSection? FindSection(string code) =>
        _sections.FirstOrDefault(section =>
            string.Equals(section.Code, code, StringComparison.OrdinalIgnoreCase));

    /// <summary>Автосохранение одной секции редактора.</summary>
    public DocumentSection FillSection(
        string? code,
        string? dataJson,
        DateTime utcNow,
        string? updatedBy = null)
    {
        EnsureContentEditable("Нельзя изменить архивный документ.");

        var normalizedCode = Guard.Required(code, "Код раздела", DocumentSection.CodeMaxLength);
        var section = FindSection(normalizedCode)
                      ?? throw new NotFoundException(ErrorCodes.NotFound, $"Раздел «{normalizedCode}» не найден.");

        section.Fill(dataJson, utcNow, updatedBy);
        Reopen();

        return section;
    }

    /// <summary>Ручной снапшот содержимого. Номер версии — следующий по порядку.</summary>
    public DocumentVersion AddVersion(
        string? dataJson,
        DateTime utcNow,
        string? comment = null,
        bool isAuto = false,
        string? createdBy = null)
    {
        if (IsArchived)
            throw new ConflictException(ErrorCodes.DocumentNotEditable, "Нельзя изменить архивный документ.");

        var version = new DocumentVersion(
            NextVersionNumber(), dataJson, Status, utcNow, comment, isAuto, createdBy);

        _versions.Add(version);
        return version;
    }

    /// <summary>
    /// Автоматический снапшот текущего содержимого перед восстановлением:
    /// иначе восстановление затирает правки без возможности вернуться.
    /// </summary>
    public DocumentVersion SnapshotBeforeRestore(string? dataJson, DateTime utcNow, string? createdBy = null) =>
        AddVersion(dataJson, utcNow, comment: "Перед восстановлением версии", isAuto: true, createdBy: createdBy);

    public DocumentVersion FindVersion(int number) =>
        _versions.FirstOrDefault(version => version.Number == number)
        ?? throw new NotFoundException(ErrorCodes.NotFound, $"Версия {number} не найдена.");

    /// <summary>Регистрация импорта: исходный файл прикрепляется отдельно.</summary>
    public DocumentImport RecordImport(
        string? fileName,
        string? detectedSchemaCode,
        int importedFields,
        string? warningsJson,
        DateTime utcNow,
        string? createdBy = null)
    {
        EnsureContentEditable("Нельзя изменить архивный документ.");

        if (detectedSchemaCode is not null
            && !string.Equals(detectedSchemaCode, SchemaCode, StringComparison.OrdinalIgnoreCase))
            throw new ConflictException(
                ErrorCodes.SchemaMismatch,
                $"Файл относится к схеме {detectedSchemaCode}, документ заполняется по схеме {SchemaCode}.");

        var import = new DocumentImport(fileName, detectedSchemaCode, importedFields, warningsJson, utcNow, createdBy);
        _imports.Add(import);
        Reopen();

        return import;
    }

    /// <summary>
    /// Результат последней проверки. Он же меняет статус: «проверен» или «есть ошибки».
    /// История проверок не хранится — она нужна только для отладки и живёт в логах.
    /// </summary>
    public void RecordCheck(
        int errorCount,
        int warningCount,
        string? errorsJson,
        DateTime utcNow,
        string? createdBy = null)
    {
        if (IsArchived)
            throw new ConflictException(ErrorCodes.DocumentNotEditable, "Нельзя изменить архивный документ.");

        if (errorCount < 0 || warningCount < 0)
            throw new ValidationException(ErrorCodes.Validation, "Счётчики замечаний не могут быть отрицательными.");

        CheckedAt = utcNow;
        CheckErrorCount = errorCount;
        CheckWarningCount = warningCount;
        CheckErrorsJson = Guard.Optional(errorsJson, "Замечания", CheckDetailsMaxLength);
        Status = errorCount == 0 ? DocumentStatus.Verified : DocumentStatus.HasErrors;

        Touch(utcNow, createdBy);
    }

    /// <summary>XML можно выгрузить только после успешной проверки.</summary>
    public void RecordXml(
        string? sha256,
        long sizeBytes,
        bool valid,
        DateTime utcNow,
        string? updatedBy = null)
    {
        if (Status != DocumentStatus.Verified)
            throw new ConflictException(
                ErrorCodes.DocumentNotVerified,
                "XML выгружается только для проверенного документа.");

        if (sizeBytes <= 0)
            throw new ValidationException(ErrorCodes.Validation, "Размер XML должен быть больше нуля.");

        XmlSha256 = Guard.Required(sha256, "Хеш XML", XmlSha256Length);
        XmlSizeBytes = sizeBytes;
        XmlValid = valid;

        Touch(utcNow, updatedBy);
    }

    public void Archive(DateTime utcNow, string? updatedBy = null)
    {
        if (IsArchived)
            return;

        IsArchived = true;
        Status = DocumentStatus.Archived;
        Touch(utcNow, updatedBy);
    }

    public void Restore(DateTime utcNow, string? updatedBy = null)
    {
        if (!IsArchived)
            return;

        IsArchived = false;
        Status = DocumentStatus.Draft;
        Touch(utcNow, updatedBy);
    }

    /// <summary>Черновик — единственный статус, в котором документ правится.</summary>
    private void EnsureEditable(string message)
    {
        if (IsArchived || Status != DocumentStatus.Draft)
            throw new ConflictException(ErrorCodes.DocumentNotEditable, message);
    }

    /// <summary>
    /// Правка содержимого: разрешена и в проверенном документе, но снимает отметку проверки —
    /// после изменения прежний результат уже ничего не гарантирует.
    /// </summary>
    private void EnsureContentEditable(string message)
    {
        if (IsArchived)
            throw new ConflictException(ErrorCodes.DocumentNotEditable, message);
    }

    private void Reopen()
    {
        if (Status is DocumentStatus.Verified or DocumentStatus.HasErrors)
            Status = DocumentStatus.Draft;

        // Результат проверки относится к прежнему содержимому: после правки он недействителен.
        CheckedAt = null;
        CheckErrorCount = 0;
        CheckWarningCount = 0;
        CheckErrorsJson = null;
        XmlSha256 = null;
        XmlSizeBytes = null;
        XmlValid = null;
    }

    private int NextVersionNumber() =>
        _versions.Count == 0 ? 1 : _versions.Max(version => version.Number) + 1;
}
