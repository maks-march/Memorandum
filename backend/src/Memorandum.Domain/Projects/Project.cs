using Memorandum.Domain.Common;
using Memorandum.Domain.Organizations;

namespace Memorandum.Domain.Projects;

/// <summary>
/// Проект организации — объект проектирования, под которым создаются пояснительные
/// записки (ТЗ, п. 5.2). Проект — агрегат: участники (сотрудники той же организации)
/// меняются только через него.
/// </summary>
public sealed class Project : AuditableEntity
{
    public const int NameMaxLength = 200;
    public const int ObjectNameMaxLength = 500;
    public const int AddressMaxLength = 500;
    public const int CodeMaxLength = 50;
    public const int RegionCodeMaxLength = 2;
    public const int OktmoCodeMaxLength = 11;
    public const int OktmoNameMaxLength = 200;

    private readonly List<ProjectMember> _members = [];

    private Project()
    {
    }

    public long OrganizationId { get; private set; }

    public Organization Organization { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    /// <summary>Наименование объекта капитального строительства.</summary>
    public string ObjectName { get; private set; } = null!;

    /// <summary>Шифр проекта в организации (например «1234-ИОС»), необязателен.</summary>
    public string? Code { get; private set; }

    /// <summary>Местоположение проектируемого объекта.</summary>
    public string? AddressText { get; private set; }

    /// <summary>Ответственный сотрудник: на него ориентируются при поиске документов.</summary>
    public long? ResponsibleUserId { get; private set; }

    /// <summary>Вид работ: строительство, реконструкция, капремонт, снос.</summary>
    public WorkType WorkType { get; private set; } = WorkType.Construction;

    /// <summary>Тип объекта: линейный, производственный, непроизводственный.</summary>
    public ObjectKind ObjectKind { get; private set; } = ObjectKind.NonIndustrial;

    /// <summary>Код субъекта РФ: входит в адрес объекта и в XML.</summary>
    public string? RegionCode { get; private set; }

    /// <summary>Код ОКТМО: с версии схемы 01.06 обязателен в XML.</summary>
    public string? OktmoCode { get; private set; }

    public string? OktmoName { get; private set; }

    public bool IsArchived { get; private set; }

    /// <summary>Актуальный состав участников (удалённые остаются в коллекции неактивными).</summary>
    public IReadOnlyCollection<ProjectMember> Members => _members;

    public static Project Create(
        Organization organization,
        string? name,
        string? objectName,
        WorkType workType,
        ObjectKind objectKind,
        DateTime utcNow,
        string? addressText = null,
        string? code = null,
        string? regionCode = null,
        string? oktmoCode = null,
        string? oktmoName = null,
        string? createdBy = null)
    {
        ArgumentNullException.ThrowIfNull(organization);

        if (!organization.IsActive)
            throw new ConflictException(ErrorCodes.OrganizationInactive, "Организация деактивирована.");

        var project = new Project
        {
            Organization = organization,
            OrganizationId = organization.Id,
            IsArchived = false,
        };

        project.Apply(name, objectName, workType, objectKind,
            addressText, code, regionCode, oktmoCode, oktmoName);

        project.MarkCreated(utcNow, createdBy);
        return project;
    }

    /// <summary>Правка карточки проекта. Организация и архив меняются отдельными методами.</summary>
    public void Update(
        Organization organization,
        string? name,
        string? objectName,
        WorkType workType,
        ObjectKind objectKind,
        DateTime utcNow,
        string? addressText = null,
        string? code = null,
        string? regionCode = null,
        string? oktmoCode = null,
        string? oktmoName = null,
        string? updatedBy = null)
    {
        ArgumentNullException.ThrowIfNull(organization);
        EnsureNotArchived("Нельзя изменить архивный проект.");

        Apply(name, objectName, workType, objectKind,
            addressText, code, regionCode, oktmoCode, oktmoName);

        Touch(utcNow, updatedBy);
    }

    /// <summary>Код субъекта РФ: формат важнее длины, иначе «660» даст ошибку длины.</summary>
    internal static string? NormalizeRegion(string? regionCode) =>
        Guard.Optional(regionCode, "Код региона") is { } region
            ? Guard.Matches(region, "Код региона", Guard.RegionCode(), "две цифры")
            : null;

    /// <summary>Код ОКТМО: 8 или 11 цифр.</summary>
    internal static string? NormalizeOktmo(string? oktmoCode) =>
        Guard.Optional(oktmoCode, "Код ОКТМО") is { } oktmo
            ? Guard.Matches(oktmo, "Код ОКТМО", Guard.Oktmo(), "8 или 11 цифр")
            : null;

    /// <summary>Поля карточки без аудита: общие для создания и правки.</summary>
    private void Apply(
        string? name,
        string? objectName,
        WorkType workType,
        ObjectKind objectKind,
        string? addressText,
        string? code,
        string? regionCode,
        string? oktmoCode,
        string? oktmoName)
    {
        Name = Guard.Required(name, "Наименование проекта", NameMaxLength);
        ObjectName = Guard.Required(objectName, "Наименование объекта", ObjectNameMaxLength);
        AddressText = Guard.Optional(addressText, "Адрес объекта", AddressMaxLength);
        Code = Guard.Optional(code, "Шифр проекта", CodeMaxLength);
        WorkType = workType;
        ObjectKind = objectKind;

        RegionCode = NormalizeRegion(regionCode);
        OktmoCode = NormalizeOktmo(oktmoCode);

        OktmoName = Guard.Optional(oktmoName, "Наименование ОКТМО", OktmoNameMaxLength);
    }

    /// <summary>Ответственный — только активный участник проекта: иначе он не увидит документы.</summary>
    public void AssignResponsible(long? userId, DateTime utcNow, string? updatedBy = null)
    {
        EnsureNotArchived("Нельзя изменить архивный проект.");

        if (userId is not null && !HasMember(userId.Value))
            throw new ValidationException(
                ErrorCodes.ProjectMemberRequired,
                "Ответственным можно назначить только участника проекта.");

        ResponsibleUserId = userId;
        Touch(utcNow, updatedBy);
    }

    /// <summary>
    /// Добавляет участника. Обязательное условие — сотрудник той же организации:
    /// проект и его документы не должны утекать в чужую организацию.
    /// </summary>
    public ProjectMember AddMember(long userId, long projectOrganizationId, DateTime utcNow, string? createdBy = null)
    {
        EnsureNotArchived("Нельзя изменить состав участников архивного проекта.");

        if (projectOrganizationId != OrganizationId)
            throw new ValidationException(
                ErrorCodes.OrganizationMismatch,
                "Участник должен состоять в организации проекта.");

        var existing = _members.FirstOrDefault(member => member.UserId == userId);

        if (existing is { IsActive: true })
            throw new ConflictException(ErrorCodes.MemberAlreadyExists, "Сотрудник уже участвует в проекте.");

        if (existing is not null)
        {
            existing.Reactivate(utcNow, createdBy);
            return existing;
        }

        var added = new ProjectMember(userId, utcNow, createdBy);
        _members.Add(added);
        return added;
    }

    public void RemoveMember(long userId, DateTime utcNow, string? updatedBy = null)
    {
        EnsureNotArchived("Нельзя изменить состав участников архивного проекта.");

        var member = _members.FirstOrDefault(m => m.UserId == userId && m.IsActive)
                     ?? throw new NotFoundException(ErrorCodes.NotFound, "Сотрудник не участвует в проекте.");

        member.Deactivate(utcNow, updatedBy);

        // Ответственный, выбывший из проекта, перестаёт быть ответственным:
        // иначе проект ссылается на того, у кого нет доступа.
        if (ResponsibleUserId == userId)
            ResponsibleUserId = null;
    }

    /// <summary>
    /// Доступ к проекту: сотрудник той же организации и не архивный проект.
    /// Состав участников (кто именно работает) — отдельный, более узкий вопрос.
    /// </summary>
    public bool CanBeWorkedOnBy(long userId, long userOrganizationId) =>
        !IsArchived && userOrganizationId == OrganizationId && Organization is { IsActive: true } && userId > 0;

    public bool HasMember(long userId) =>
        userId > 0 && _members.Any(member => member.IsActive && member.UserId == userId);

    /// <summary>Архивный проект доступен только для чтения: правки требуют восстановления.</summary>
    public void Archive(DateTime utcNow, string? updatedBy = null)
    {
        if (IsArchived)
            return;

        IsArchived = true;
        Touch(utcNow, updatedBy);
    }

    public void Restore(DateTime utcNow, string? updatedBy = null)
    {
        if (!IsArchived)
            return;

        IsArchived = false;
        Touch(utcNow, updatedBy);
    }

    private void EnsureNotArchived(string message)
    {
        if (IsArchived)
            throw new ConflictException(ErrorCodes.ProjectArchived, message);
    }
}
