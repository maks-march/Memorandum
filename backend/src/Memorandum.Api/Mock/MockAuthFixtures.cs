namespace Mocks;

/// <summary>
/// Данные мока авторизации.
/// Каждый вызов возвращает новые экземпляры — стор берёт их как начальное состояние.
/// </summary>
internal static class MockAuthFixtures
{
    internal const string Password = "password";

    private static readonly Guid OrgFirstId = new("a1b2c3d4-0000-4000-8000-000000000001");
    private static readonly Guid OrgSecondId = new("a1b2c3d4-0000-4000-8000-000000000002");


    public static IReadOnlyList<MockOrg> Orgs()
    {
        return [new MockOrg(
            OrgFirstId,
            "ACME 1"
        ), new MockOrg(
            OrgSecondId,
            "ACME 2"
        )];
    }
    public static IReadOnlyList<MockUser> Users()
    {
        return [
            new MockUser(
                new Guid("b1b2c3d4-0000-4000-8000-000000000001"),
                "sa@example.ru",
                Password,
                "Сергей Админов",
                "Администратор сервиса",
                "SA",
                null),
            new MockUser(
                new Guid("b1b2c3d4-0000-4000-8000-000000000002"),
                "oa@example.ru",
                Password,
                "Ольга Админова",
                "Главный инженер проекта",
                "OA",
                OrgFirstId),
            new MockUser(
                new Guid("b1b2c3d4-0000-4000-8000-000000000003"),
                "ed@example.ru",
                Password,
                "Егор Дроздов",
                "Инженер-проектировщик",
                "ED",
                OrgSecondId),
        ];
    }
}

public sealed record MockOrg(Guid Id, string Name);

internal sealed record MockUser(
    Guid Id,
    string Email,
    string Password,
    string FullName,
    string Position,
    string Role,
    Guid? OrgId);

public sealed record MockSession(Guid UserId, DateTimeOffset ExpiresAt, string XsrfToken);
