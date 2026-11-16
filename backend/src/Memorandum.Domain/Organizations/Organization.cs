using Memorandum.Domain.Common;
using Memorandum.Domain.Users;

namespace Memorandum.Domain.Organizations;

/// <summary>Организация — агрегат, владеющий участием пользователей.</summary>
public sealed class Organization : AuditableEntity
{
    public const int NameMaxLength = 200;

    private readonly List<OrganizationMembership> _memberships = [];

    private Organization()
    {
    }

    public string Name { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<OrganizationMembership> Memberships => _memberships;

    public static Organization Create(string? name, string? createdBy = null)
    {
        var organization = new Organization
        {
            Name = Guard.Required(name, "Название организации", NameMaxLength),
            IsActive = true,
        };

        organization.MarkCreated(createdBy);
        return organization;
    }

    public void Rename(string? name, string? updatedBy = null)
    {
        Name = Guard.Required(name, "Название организации", NameMaxLength);
        Touch(updatedBy);
    }

    /// <summary>
    /// Добавляет участника. 
    /// Ранее исключённый пользователь возвращается с новой ролью — второй записи на того же пользователя не появляется.
    /// </summary>
    public OrganizationMembership AddMember(User user, OrganizationRole role, string? createdBy = null)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (!IsActive)
            throw new ConflictException(ErrorCodes.OrganizationInactive, "Организация деактивирована.");

        var existing = _memberships.FirstOrDefault(m => m.UserId == user.Id && user.Id > 0)
                       ?? _memberships.FirstOrDefault(m => ReferenceEquals(m.User, user));

        if (existing is { IsActive: true })
            throw new ConflictException(ErrorCodes.MemberAlreadyExists, "Пользователь уже состоит в организации.");

        if (existing is not null)
        {
            existing.Reactivate(role, createdBy);
            return existing;
        }

        var membership = new OrganizationMembership(user, role, createdBy);
        _memberships.Add(membership);
        return membership;
    }

    public void ChangeRole(long userId, OrganizationRole role, string? updatedBy = null)
    {
        var membership = FindActive(userId);

        if (membership.Role == role)
            return;

        EnsureNotLastAdmin(membership, "Нельзя понизить последнего администратора организации.");

        membership.ChangeRole(role, updatedBy);
    }

    /// <summary>Исключает пользователя; последнего администратора исключить нельзя.</summary>
    public void RemoveMember(long userId, string? updatedBy = null)
    {
        var membership = FindActive(userId);
        EnsureNotLastAdmin(membership, "Нельзя исключить последнего администратора организации.");
        membership.Deactivate(updatedBy);
    }

    /// <summary>Организация больше не принимает участников.</summary>
    public void Deactivate(string? updatedBy = null)
    {
        IsActive = false;
        Touch(updatedBy);
    }

    private OrganizationMembership FindActive(long userId)
    {
        Guard.PositiveId(userId, "Пользователь");

        var membership = _memberships.FirstOrDefault(m => m.UserId == userId && m.IsActive)
                         ?? throw new NotFoundException(ErrorCodes.NotFound, "Пользователь не состоит в организации.");

        return membership;
    }

    private void EnsureNotLastAdmin(OrganizationMembership membership, string message)
    {
        var admins = _memberships.Count(m => m.IsActive && m.Role == OrganizationRole.OrgAdmin);

        if (membership.Role == OrganizationRole.OrgAdmin && admins == 1)
            throw new ConflictException(ErrorCodes.LastOrgAdminRequired, message);
    }
}
