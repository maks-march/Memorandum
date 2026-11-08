using Memorandum.Api.Domain.Users;

namespace Memorandum.Api.Domain.Organizations;

/// <summary>Организация — агрегат, владеющий членством пользователей.</summary>
public sealed class Organization
{
    private readonly List<OrganizationMembership> _memberships = [];

    private Organization()
    {
    }

    /// <summary>Идентификатор, генерируется EF.</summary>
    public long Id { get; private set; }

    public string Name { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<OrganizationMembership> Memberships => _memberships;

    public static Fin<Organization> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ApiErrors.OrganizationNameRequired;

        return new Organization { Name = name.Trim(), IsActive = true };
    }

    public Fin<OrganizationMembership> AddMember(long userId, OrganizationRole role)
    {
        if (!IsActive)
            return ApiErrors.OrganizationInactive;

        if (_memberships.Any(m => m.UserId == userId))
            return ApiErrors.MemberAlreadyExists;

        var membership = new OrganizationMembership(userId, role);
        _memberships.Add(membership);
        return membership;
    }

    public Fin<Unit> RemoveMember(long userId)
    {
        if (FindMembership(userId) is not { } membership)
            return ApiErrors.MemberNotFound;

        if (IsLastOrgAdmin(membership))
            return ApiErrors.LastOrgAdmin;

        _memberships.Remove(membership);
        return Unit.Default;
    }

    public Fin<Unit> ChangeRole(User user, OrganizationRole role)
    {
        if (FindMembership(user.Id) is not { } membership)
            return ApiErrors.MemberNotFound;

        if (membership.Role == role)
            return Unit.Default;

        if (membership.Role == OrganizationRole.OrgAdmin && IsLastOrgAdmin(membership))
            return ApiErrors.LastOrgAdmin;

        membership.ChangeRole(role);
        user.RotateSecurityStamp();
        return Unit.Default;
    }

    /// <summary>Организация больше не принимает участников.</summary>
    public void Deactivate() => IsActive = false;

    private OrganizationMembership? FindMembership(long userId) =>
        _memberships.FirstOrDefault(m => m.UserId == userId);

    private bool IsLastOrgAdmin(OrganizationMembership membership) =>
        membership.Role == OrganizationRole.OrgAdmin &&
        _memberships.Count(m => m.Role == OrganizationRole.OrgAdmin) == 1;
}
