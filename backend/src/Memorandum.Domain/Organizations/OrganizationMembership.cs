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

    internal OrganizationMembership(User user, OrganizationRole role, DateTime utcNow, string? createdBy)
    {
        User = user ?? throw new ArgumentNullException(nameof(user));
        UserId = user.Id;
        Role = role;
        IsActive = true;
        MarkCreated(utcNow, createdBy);
    }

    public long UserId { get; private set; }

    public User User { get; private set; } = null!;

    public long OrganizationId { get; private set; }

    public Organization Organization { get; private set; } = null!;

    public OrganizationRole Role { get; private set; }

    public bool IsActive { get; private set; }

    internal void ChangeRole(OrganizationRole role, DateTime utcNow, string? updatedBy)
    {
        Role = role;
        Touch(utcNow, updatedBy);
    }

    /// <summary>Исключение из организации: участие остаётся в истории неактивным.</summary>
    internal void Deactivate(DateTime utcNow, string? updatedBy)
    {
        IsActive = false;
        Touch(utcNow, updatedBy);
    }

    /// <summary>Повторное добавление ранее исключённого пользователя.</summary>
    internal void Reactivate(OrganizationRole role, DateTime utcNow, string? updatedBy)
    {
        Role = role;
        IsActive = true;
        Touch(utcNow, updatedBy);
    }
}
