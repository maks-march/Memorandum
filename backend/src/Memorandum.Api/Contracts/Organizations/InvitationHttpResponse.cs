
namespace Memorandum.Api.Contracts.Organizations;

/// <summary>Приглашение сотрудника по email: <c>/api/v1/invitations</c>.</summary>
/// <param name="Id">Идентификатор приглашения.</param>
/// <param name="Email">Адрес приглашённого.</param>
/// <param name="Role">Роль, которая будет выдана.</param>
/// <param name="Status">Pending, Accepted или Revoked.</param>
/// <param name="ExpiresAt">Когда приглашение перестанет действовать.</param>
public sealed record InvitationHttpResponse(
    long Id,
    string Email,
    string Role,
    string Status,
    DateTime ExpiresAt)
{
    /// <summary>Моковая версия для <c>/api/v1/mocks/invitations</c>.</summary>
    public static InvitationHttpResponse Mock(long id = 1) =>
        new(
            id,
            "ivanov@example.ru",
            "Editor",
            "Pending",
            new DateTime(2026, 3, 2, 9, 0, 0, DateTimeKind.Utc));

    public static IReadOnlyList<InvitationHttpResponse> MockList() =>
        [Mock(1), Mock(2) with { Email = "petrova@example.ru", Role = "Viewer" }];
}
