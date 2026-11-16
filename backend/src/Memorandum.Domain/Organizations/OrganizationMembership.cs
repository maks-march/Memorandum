using Memorandum.Domain.Common;
using Memorandum.Domain.Users;

namespace Memorandum.Domain.Organizations;

/// <summary>
/// Участие пользователя в организации. Часть агрегата <see cref="Organization"/>:
/// создаётся и меняется только через неё.
/// </summary>
public sealed class OrganizationMembership : AuditableEntity
{
    private OrganizationMembership()
    {
    }

    internal OrganizationMembership(User user, OrganizationRole role, string? createdBy)
    {
        User = user ?? throw new ArgumentNullException(nameof(user));
        UserId = user.Id;
        Role = role;
        IsActive = true;
        MarkCreated(createdBy);
    }

    public long UserId { get; private set; }

    public User User { get; private set; } = null!;

    public long OrganizationId { get; private set; }

    public Organization Organization { get; private set; } = null!;

    public OrganizationRole Role { get; private set; }

    public bool IsActive { get; private set; }

    internal void ChangeRole(OrganizationRole role, string? updatedBy)
    {
        Role = role;
        Touch(updatedBy);
    }

    /// <summary>Исключение из организации: участие остаётся в истории неактивным.</summary>
    internal void Deactivate(string? updatedBy)
    {
        IsActive = false;
        Touch(updatedBy);
    }

    /// <summary>Повторное добавление ранее исключённого пользователя.</summary>
    internal void Reactivate(OrganizationRole role, string? updatedBy)
    {
        Role = role;
        IsActive = true;
        Touch(updatedBy);
    }
}
