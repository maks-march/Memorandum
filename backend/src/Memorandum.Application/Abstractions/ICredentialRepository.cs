using Memorandum.Domain.Interfaces;
using Memorandum.Domain.Users;

namespace Memorandum.Application.Abstractions;

public interface ICredentialRepository : IRepository<Credential>
{
    Task<Credential?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default);
}
