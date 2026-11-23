using Memorandum.Domain.Organizations;

namespace Memorandum.Application.Invitations.Models;

/// <summary>
/// Код приглашения для показа администратору. Код возвращается открытым текстом
/// ровно один раз — в ответе на выдачу; в хранилище лежит только хеш.
/// </summary>
/// <param name="OrganizationId">Организация, в которую приглашают.</param>
/// <param name="OrganizationName">Её название — чтобы код не перепутали.</param>
/// <param name="Code">Сам код; показывается один раз.</param>
/// <param name="Role">Роль, которую получит приглашённый.</param>
/// <param name="ExpiresAt">Когда код перестанет действовать.</param>
public sealed record InviteCodeDto(
    long OrganizationId,
    string OrganizationName,
    string Code,
    OrganizationRole Role,
    DateTimeOffset ExpiresAt);
