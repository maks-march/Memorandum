using Memorandum.Domain.Organizations;

namespace Memorandum.Application.Invitations;

/// <summary>
/// Действующий код приглашения. Открытый код не хранится — только хеш:
/// посмотреть код ещё раз нельзя, при утере выдаётся новый.
/// </summary>
/// <param name="OrganizationId">Организация, в которую приглашают.</param>
/// <param name="CodeHash">HMAC от кода с серверным pepper.</param>
/// <param name="Role">Роль приглашённого: администратора по коду не выдают.</param>
/// <param name="ExpiresAt">Когда код перестанет действовать.</param>
public sealed record InviteCodeEntry(
    long OrganizationId,
    string CodeHash,
    OrganizationRole Role,
    DateTimeOffset ExpiresAt);
