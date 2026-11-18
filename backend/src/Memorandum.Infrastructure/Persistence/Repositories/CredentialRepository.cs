using Microsoft.EntityFrameworkCore;
using Memorandum.Application.Abstractions;
using Memorandum.Domain.Users;

namespace Memorandum.Infrastructure.Persistence.Repositories;

public sealed class CredentialRepository(AppDbContext db) : EfRepository<Credential>(db), ICredentialRepository
{
    /// <summary>Отслеживаемый: при устаревшем хеше сценарий входа сразу его обновляет.</summary>
    public Task<Credential?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(credential => credential.UserId == userId, cancellationToken);
}
