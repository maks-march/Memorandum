using Memorandum.Domain.Interfaces;
using Memorandum.Domain.Users;

namespace Memorandum.Application.Abstractions;

/// <summary>Запросы к пользователям, нужные сценариям. Чтение — без отслеживания изменений.</summary>
public interface IUserRepository : IRepository<User>
{
    Task<bool> ExistsByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    /// <summary>Отслеживаемый пользователь: нужен сценариям, которые меняют состояние.</summary>
    Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);
}
