using Memorandum.Application.Invitations.Models;

namespace Memorandum.Api.Contracts;

/// <summary>
/// Ответ на выдачу кода приглашения. Код показывается один раз: в хранилище
/// лежит только его хеш, повторно посмотреть нельзя — можно выдать новый.
/// </summary>
public sealed record InviteCodeHttpResponse(
    long OrganizationId,
    string OrganizationName,
    string Code,
    string Role,
    DateTimeOffset ExpiresAt)
{
    public static InviteCodeHttpResponse From(InviteCodeDto dto) =>
        new(dto.OrganizationId, dto.OrganizationName, dto.Code, dto.Role.ToString(), dto.ExpiresAt);
}
