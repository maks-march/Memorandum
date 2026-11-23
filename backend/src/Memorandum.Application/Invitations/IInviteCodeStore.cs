namespace Memorandum.Application.Invitations;

/// <summary>
/// Хранилище кодов приглашения. Живёт в Redis: код одноразовый, умирает сам
/// по TTL, и в БД для него заводить таблицу незачем.
/// </summary>
public interface IInviteCodeStore
{
    /// <summary>
    /// Кладёт код на место предыдущего: у организации всегда один действующий код,
    /// старый перестаёт работать в момент выдачи нового.
    /// </summary>
    Task SaveAsync(
        InviteCodeEntry entry,
        string code,
        TimeSpan timeToLive,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Проверяет код и при успехе удаляет его: повторно воспользоваться нельзя.
    /// Реализация обязана быть атомарной, иначе два одновременных запроса
    /// с одним кодом оба создадут пользователя.
    /// </summary>
    /// <returns>Приглашение или <c>null</c>, если код не найден, истёк или уже использован.</returns>
    Task<InviteCodeEntry?> RedeemAsync(string code, CancellationToken cancellationToken = default);
}
