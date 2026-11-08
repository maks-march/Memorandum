namespace Memorandum.Api.Domain.Organizations;

/// <summary>Членство пользователя в организации. Часть агрегата <see cref="Organization"/>.</summary>
public sealed class OrganizationMembership
{
    private OrganizationMembership()
    {
    }

    internal OrganizationMembership(long userId, OrganizationRole role)
    {
        UserId = userId;
        Role = role;
    }

    public long Id { get; private set; }

    public long OrganizationId { get; private set; }

    public long UserId { get; private set; }

    public OrganizationRole Role { get; private set; }

    internal void ChangeRole(OrganizationRole role) => Role = role;
}
