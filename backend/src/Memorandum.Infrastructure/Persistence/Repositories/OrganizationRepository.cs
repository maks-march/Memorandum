using Microsoft.EntityFrameworkCore;
using Memorandum.Application.Abstractions;
using Memorandum.Domain.Organizations;

namespace Memorandum.Infrastructure.Persistence.Repositories;

public sealed class OrganizationRepository(AppDbContext db) : EfRepository<Organization>(db), IOrganizationRepository
{
    public Task<ActiveMembership?> GetActiveMembershipAsync(
        long userId,
        CancellationToken cancellationToken = default) =>
        (from user in Db.Users.AsNoTracking()
         join membership in Db.Memberships.AsNoTracking() on user.ActiveOrganizationId equals membership.OrganizationId
         join organization in Db.Organizations.AsNoTracking() on membership.OrganizationId equals organization.Id
         where user.Id == userId && membership.UserId == userId && membership.IsActive
         select new ActiveMembership(organization.Id, organization.Name, membership.Role, membership.Position))
        .FirstOrDefaultAsync(cancellationToken);

    public override Task<Organization?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        Set.Include(organization => organization.Memberships)
            .FirstOrDefaultAsync(organization => organization.Id == id, cancellationToken);

    public Task<Organization?> GetOrganizationByInviteCode(string inviteCode, CancellationToken cancellationToken = default) => 
        Set.Include(organization => organization.Memberships)
            .FirstOrDefaultAsync(organization => organization.CheckInviteCode(inviteCode), cancellationToken);

}
