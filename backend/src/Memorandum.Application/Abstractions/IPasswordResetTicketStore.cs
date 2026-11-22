using Memorandum.Application.PasswordChange.Models;

namespace Memorandum.Application.Abstractions;

/// <summary>
/// Хранилище тикетов сброса пароля. Проверка кода — атомарная операция хранилища:
/// уменьшать счётчик попыток «снаружи» нельзя, два параллельных запроса обойдут лимит.
/// </summary>
public interface IPasswordResetTicketStore
{
    /// <summary>Ставит кулдаун повторной отправки. <c>false</c> — кулдаун ещё активен.</summary>
    Task<bool> TryStartCooldownAsync(
        long userId,
        TimeSpan cooldown,
        CancellationToken cancellationToken = default);

    /// <summary>Создаёт или перезаписывает тикет с новым кодом.</summary>
    Task SaveAsync(
        long userId,
        PasswordResetTicket ticket,
        TimeSpan timeToLive,
        CancellationToken cancellationToken = default);

    /// <summary>Атомарно проверяет код: при совпадении тикет удаляется, при ошибке уменьшается счётчик.</summary>
    Task<ConsumeCodeResult> ConsumeCodeAsync(
        long userId,
        string codeHash,
        CancellationToken cancellationToken = default);
}
