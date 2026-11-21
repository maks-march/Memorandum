using Microsoft.EntityFrameworkCore;
using Memorandum.Application.Abstractions;
using Memorandum.Domain.Users;

namespace Memorandum.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(AppDbContext db) : EfRepository<User>(db), IUserRepository
{
    public Task<bool> ExistsByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);

    /// <summary>Отслеживаемый: сценарий входа может менять состояние пользователя.</summary>
    public Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);

    /// <summary>Отслеживаемый пользователь для сценариев, которые меняют состояние.</summary>
    public Task<User?> GetForUpdateAsync(long id, CancellationToken cancellationToken = default) =>
        Set.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

    /// <summary>Профиль на показ: без попадания в change tracker.</summary>
    public override Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking().FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
}
