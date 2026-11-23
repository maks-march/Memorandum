using Memorandum.Domain.Common;
using Memorandum.Domain.Organizations;

namespace Memorandum.Domain.Projects;

/// <summary>
/// Проект организации — объект проектирования, под которым создаются пояснительные
/// записки. Проект — агрегат: участники (сотрудники той же организации)
/// меняются только через него.
/// </summary>
public sealed class Project : AuditableEntity
{
    public const int NameMaxLength = 200;
    public const int ObjectNameMaxLength = 500;
    public const int AddressMaxLength = 500;
    public const int CodeMaxLength = 50;

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

    public bool IsArchived { get; private set; }

    /// <summary>Актуальный состав участников (удалённые остаются в коллекции неактивными).</summary>
    public IReadOnlyCollection<ProjectMember> Members => _members;

    public static Project Create(
        Organization organization,
        string? name,
        string? objectName,
        DateTime utcNow,
        string? addressText = null,
        string? code = null,
        string? createdBy = null)
    {
        ArgumentNullException.ThrowIfNull(organization);

        if (!organization.IsActive)
            throw new ConflictException(ErrorCodes.OrganizationInactive, "Организация деактивирована.");

        var project = new Project
        {
            Organization = organization,
            OrganizationId = organization.Id,
            Name = Guard.Required(name, "Наименование проекта", NameMaxLength),
            ObjectName = Guard.Required(objectName, "Наименование объекта", ObjectNameMaxLength),
            AddressText = Guard.Optional(addressText, "Адрес объекта", AddressMaxLength),
            Code = Guard.Optional(code, "Шифр проекта", CodeMaxLength),
            IsArchived = false,
        };

        project.MarkCreated(utcNow, createdBy);
        return project;
    }

    /// <summary>Правка карточки проекта. Организация и архив меняются отдельными методами.</summary>
    public void Update(
        string? name,
        string? objectName,
        DateTime utcNow,
        string? addressText = null,
        string? code = null,
        string? updatedBy = null)
    {
        EnsureNotArchived("Нельзя изменить архивный проект.");

        Name = Guard.Required(name, "Наименование проекта", NameMaxLength);
        ObjectName = Guard.Required(objectName, "Наименование объекта", ObjectNameMaxLength);
        AddressText = Guard.Optional(addressText, "Адрес объекта", AddressMaxLength);
        Code = Guard.Optional(code, "Шифр проекта", CodeMaxLength);

        Touch(utcNow, updatedBy);
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
