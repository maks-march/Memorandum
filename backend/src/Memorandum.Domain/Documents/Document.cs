using Memorandum.Domain.Common;
using Memorandum.Domain.Organizations;
using Memorandum.Domain.Projects;

namespace Memorandum.Domain.Documents;

/// <summary>Пояснительная записка в рамках проекта.</summary>
/// <remarks>
/// Агрегат: содержимое по разделам, участники, версии, проверки, XML и импорт
/// меняются только через него. Содержимое документа — JSON по каталогу полей схемы,
/// поэтому структура полей задаётся версией схемы, а не БД.
/// </remarks>
public sealed class Document : AuditableEntity
{
    public const int TitleMaxLength = 200;

    private readonly List<DocumentParticipant> _participants = [];
    private readonly List<DocumentVersion> _versions = [];
    private readonly List<DocumentCheck> _checks = [];
    private readonly List<XmlArtifact> _xmlArtifacts = [];
    private readonly List<DocumentImport> _imports = [];

    private Document()
    {
    }

    public long OrganizationId { get; private set; }

    public Organization? Organization { get; private set; }

    public long ProjectId { get; private set; }

    public Project Project { get; private set; } = null!;

    public string Title { get; private set; } = null!;

    /// <summary>
    /// Версия схемы фиксируется при создании: смена схемы — это другой документ,
    /// иначе уже проверенный XML перестанет соответствовать содержимому.
    /// </summary>
    public long SchemaVersionId { get; private set; }

    public Schemas.SchemaVersion SchemaVersion { get; private set; } = null!;

    public DocumentStatus Status { get; private set; }

    /// <summary>Снимок полей объекта: перенесён из проекта при создании документа.</summary>
    public DocumentObjectSnapshot Object { get; private set; } = null!;

    /// <summary>Содержимое по разделам: JSON каталога полей схемы.</summary>
    public string DataJson { get; private set; } = "{}";

    /// <summary>Ответственный за документ; <c>null</c> — отвечает ответственный за проект.</summary>
    public long? ResponsibleUserId { get; private set; }

    /// <summary>Момент удаления: документ скрывается, но версии и XML остаются.</summary>
    public DateTime? DeletedAt { get; private set; }

    /// <summary>Статус до архивации: восстановление возвращает именно его.</summary>
    public DocumentStatus? StatusBeforeArchiving { get; private set; }

    public IReadOnlyCollection<DocumentParticipant> Participants => _participants;

    public IReadOnlyCollection<DocumentVersion> Versions => _versions;

    public IReadOnlyCollection<DocumentCheck> Checks => _checks;

    public IReadOnlyCollection<XmlArtifact> XmlArtifacts => _xmlArtifacts;

    public IReadOnlyCollection<DocumentImport> Imports => _imports;

    public bool IsDeleted => DeletedAt is not null;

    /// <summary>Есть ли успешная проверка, по которой можно выгружать XML.</summary>
    public DocumentCheck? LastCheck => _checks.Count == 0
        ? null
        : _checks.MaxBy(check => check.CheckedAt);

    public bool CanDownloadXml => Status == DocumentStatus.Verified && LastCheck is { Ok: true };

    public static Document Create(
        Project project,
        Organization organization,
        Schemas.SchemaVersion schemaVersion,
        string? title,
        DateTime utcNow,
        string? createdBy = null)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(organization);
        ArgumentNullException.ThrowIfNull(schemaVersion);

        if (project.IsArchived)
            throw new ConflictException(
                ErrorCodes.ProjectArchived,
                "Нельзя создать документ в архивном проекте.");

        if (!schemaVersion.IsActive)
            throw new ConflictException(
                ErrorCodes.SchemaInactive,
                $"Версия схемы {schemaVersion.Code} выключена: новые документы по ней не создаются.");

        var document = new Document
        {
            Organization = organization,
            OrganizationId = organization.Id,
            Project = project,
            ProjectId = project.Id,
            SchemaVersion = schemaVersion,
            SchemaVersionId = schemaVersion.Id,
            Title = Guard.Required(title, "Заголовок документа", TitleMaxLength),
            Status = DocumentStatus.Draft,
            DataJson = "{}",
            Object = DocumentObjectSnapshot.FromProject(project),
        };

        document.MarkCreated(utcNow, createdBy);
        return document;
    }

    public void Rename(string? title, DateTime utcNow, string? updatedBy = null)
    {
        EnsureNotArchived("Нельзя изменить архивный документ.");

        Title = Guard.Required(title, "Заголовок документа", TitleMaxLength);
        Touch(utcNow, updatedBy);
    }

    /// <summary>Схема документа неизменяема: попытка сменить — ошибка домена.</summary>
    public void ChangeSchema(Schemas.SchemaVersion schemaVersion)
    {
        ArgumentNullException.ThrowIfNull(schemaVersion);

        if (schemaVersion.Id != SchemaVersionId)
            throw new ConflictException(
                ErrorCodes.DocumentSchemaImmutable,
                "Версия схемы фиксируется при создании документа: смена схемы — это новый документ.");
    }

    /// <summary>Ответственным можно назначить только участника проекта: иначе он не увидит документ.</summary>
    public void AssignResponsible(long? userId, DateTime utcNow, string? updatedBy = null)
    {
        EnsureNotArchived("Нельзя изменить архивный документ.");

        if (userId is not null && !Project.HasMember(userId.Value))
            throw new ValidationException(
                ErrorCodes.ProjectMemberRequired,
                "Ответственным можно назначить только участника проекта.");

        ResponsibleUserId = userId;
        Touch(utcNow, updatedBy);
    }

    /// <summary>Автосохранение содержимого документа целиком.</summary>
    public void ReplaceData(string? dataJson, DateTime utcNow, string? updatedBy = null)
    {
        EnsureContentEditable("Нельзя изменить архивный документ.");

        if (string.IsNullOrWhiteSpace(dataJson))
            throw new ValidationException(ErrorCodes.Required, "Содержимое документа обязательно.");

        DataJson = dataJson;
        Reopen();
        Touch(utcNow, updatedBy);
    }

    /// <summary>Автосохранение одного раздела редактора.</summary>
    public void FillSection(DocumentSectionCode section, string? dataJson, DateTime utcNow, string? updatedBy = null)
    {
        EnsureContentEditable("Нельзя изменить архивный документ.");

        DataJson = DocumentData.WithSection(DataJson, section, dataJson);
        Reopen();
        Touch(utcNow, updatedBy);
    }

    /// <summary>Содержимое раздела из общего JSON; <c>null</c> — раздел не заполнялся.</summary>
    public string? SectionData(DocumentSectionCode section) => DocumentData.Section(DataJson, section);

    public DocumentParticipant AddParticipant(
        DocumentParticipantKind kind,
        PartyType partyType,
        string? fullName,
        DateTime utcNow,
        long? organizationId = null,
        long? userId = null,
        string? position = null,
        string? snils = null,
        string? nrsNopriz = null,
        int order = 0,
        string? createdBy = null)
    {
        EnsureContentEditable("Нельзя изменить архивный документ.");

        // ГИП один: иначе в XML появляются два руководителя проекта.
        if (kind == DocumentParticipantKind.Gip && _participants.Any(p => p.Kind == DocumentParticipantKind.Gip))
            throw new ConflictException(
                ErrorCodes.ParticipantDuplicateGip,
                "ГИП уже указан: заменить участника можно только через удаление прежнего.");

        var participant = new DocumentParticipant(
            this, kind, partyType, fullName, position, snils, nrsNopriz, order, organizationId, userId, utcNow, createdBy);

        _participants.Add(participant);
        Reopen();
        return participant;
    }

    public void RemoveParticipant(DocumentParticipantKind kind, DateTime utcNow, string? updatedBy = null)
    {
        EnsureContentEditable("Нельзя изменить архивный документ.");

        var participant = _participants.FirstOrDefault(item => item.Kind == kind)
                          ?? throw new NotFoundException(ErrorCodes.NotFound, $"Участник «{kind}» не указан.");

        _participants.Remove(participant);
        Reopen();
        Touch(utcNow, updatedBy);
    }

    /// <summary>Ручной снапшот содержимого. Номер версии — следующий по порядку.</summary>
    public DocumentVersion AddVersion(
        DateTime utcNow,
        string? comment = null,
        bool isAuto = false,
        string? createdBy = null)
    {
        var version = new DocumentVersion(this, NextVersionNumber(), DataJson, Status, utcNow, comment, isAuto, createdBy);
        _versions.Add(version);
        return version;
    }

    public DocumentVersion FindVersion(int number) =>
        _versions.FirstOrDefault(version => version.Number == number)
        ?? throw new NotFoundException(ErrorCodes.NotFound, $"Версия {number} не найдена.");

    /// <summary>
    /// Восстановление версии: сначала автоснапшот текущего содержимого,
    /// иначе правки затираются без возможности вернуться.
    /// </summary>
    public DocumentVersion RestoreVersion(int number, DateTime utcNow, string? createdBy = null)
    {
        EnsureContentEditable("Нельзя изменить архивный документ.");

        var version = FindVersion(number);
        var snapshot = AddVersion(utcNow, comment: "Перед восстановлением версии", isAuto: true, createdBy: createdBy);

        DataJson = version.DataJson;
        Reopen();
        Touch(utcNow, createdBy);

        return snapshot;
    }

    /// <summary>
    /// Результат проверки. История проверок append-only: статус меняет последняя,
    /// выставить «проверен» вручную нельзя.
    /// </summary>
    public DocumentCheck RecordCheck(
        bool ok,
        IReadOnlyCollection<DocumentCheckIssue> issues,
        DateTime utcNow,
        string? createdBy = null)
    {
        ArgumentNullException.ThrowIfNull(issues);

        if (IsArchived)
            throw new ConflictException(ErrorCodes.DocumentArchived, "Нельзя проверить архивный документ.");

        var check = new DocumentCheck(this, SchemaVersionId, ok, issues, utcNow, createdBy);
        _checks.Add(check);

        Status = ok ? DocumentStatus.Verified : DocumentStatus.HasErrors;
        Touch(utcNow, createdBy);

        return check;
    }

    /// <summary>Артефакт XML: сохраняется только для проверенного документа.</summary>
    public XmlArtifact RecordXml(
        DocumentCheck check,
        string? sha256,
        long sizeBytes,
        DateTime utcNow,
        string? createdBy = null)
    {
        ArgumentNullException.ThrowIfNull(check);

        if (check.DocumentId != Id || check != LastCheck && check.CheckedAt < (LastCheck?.CheckedAt ?? utcNow))
            throw new ConflictException(ErrorCodes.StaleVersion, "XML собирается по актуальной проверке документа.");

        if (!check.Ok || Status != DocumentStatus.Verified)
            throw new ConflictException(
                ErrorCodes.DocumentNotVerified,
                "XML выгружается только для документа с успешной проверкой.");

        var artifact = new XmlArtifact(this, check, sha256, sizeBytes, utcNow, createdBy);
        _xmlArtifacts.Add(artifact);

        Touch(utcNow, createdBy);
        return artifact;
    }

    /// <summary>Импорт XML: исходный файл сохраняется всегда, даже при частичном переносе.</summary>
    public DocumentImport RecordImport(
        ImportStatus status,
        string? fileName,
        string? detectedSchemaCode,
        int importedFields,
        DateTime utcNow,
        IReadOnlyCollection<string>? warnings = null,
        byte[]? originalFile = null,
        string? createdBy = null)
    {
        EnsureContentEditable("Нельзя изменить архивный документ.");

        var import = new DocumentImport(
            this, fileName, detectedSchemaCode, status, importedFields, warnings ?? [], originalFile, utcNow, createdBy);

        _imports.Add(import);
        Reopen();
        return import;
    }

    public void Archive(DateTime utcNow, string? updatedBy = null)
    {
        if (IsArchived)
            return;

        StatusBeforeArchiving = Status;
        Status = DocumentStatus.Archived;
        Touch(utcNow, updatedBy);
    }

    /// <summary>Восстановление возвращает прежний статус, а не «черновик».</summary>
    public void Unarchive(DateTime utcNow, string? updatedBy = null)
    {
        if (!IsArchived)
            return;

        Status = StatusBeforeArchiving ?? DocumentStatus.Draft;
        StatusBeforeArchiving = null;
        Touch(utcNow, updatedBy);
    }

    /// <summary>Мягкое удаление: версии, проверки и артефакты остаются в БД.</summary>
    public void Delete(DateTime utcNow, string? updatedBy = null)
    {
        if (IsDeleted)
            return;

        DeletedAt = utcNow;
        Touch(utcNow, updatedBy);
    }

    public void Restore(DateTime utcNow, string? updatedBy = null)
    {
        if (!IsDeleted)
            return;

        DeletedAt = null;
        Touch(utcNow, updatedBy);
    }

    /// <summary>Архив: содержимое, участники и импорт заблокированы.</summary>
    public bool IsArchived => Status == DocumentStatus.Archived;

    /// <summary>Черновик и статусы проверки правятся; архив — нет.</summary>
    private void EnsureContentEditable(string message)
    {
        if (IsArchived)
            throw new ConflictException(ErrorCodes.DocumentArchived, message);
    }

    private void EnsureNotArchived(string message)
    {
        if (IsArchived)
            throw new ConflictException(ErrorCodes.DocumentArchived, message);
    }

    /// <summary>
    /// Правка содержимого снимает отметку проверки: прежний результат относился
    /// к другому содержимому, а выгружать устаревший XML нельзя.
    /// </summary>
    private void Reopen()
    {
        if (Status is DocumentStatus.Verified or DocumentStatus.HasErrors)
            Status = DocumentStatus.Draft;
    }

    private int NextVersionNumber() =>
        _versions.Count == 0 ? 1 : _versions.Max(version => version.Number) + 1;
}
