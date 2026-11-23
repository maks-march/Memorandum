using Memorandum.Domain.Interfaces;
using Memorandum.Domain.Organizations;

namespace Memorandum.Application.Abstractions;

/// <summary>Активное участие пользователя: организация и его роль в ней.</summary>
public sealed record ActiveMembership(
    long OrganizationId,
    string OrganizationName,
    OrganizationRole Role,
    string? Position);

public interface IOrganizationRepository : IRepository<Organization>
{
    /// <summary>Участие в организации, выбранной пользователем как текущая.</summary>
    Task<ActiveMembership?> GetActiveMembershipAsync(long userId, CancellationToken cancellationToken = default);
}
