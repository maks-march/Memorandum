using Memorandum.Domain.Users;

namespace Memorandum.Application.Auth;

/// <summary>Профиль, который возвращается после регистрации. Секретов здесь нет.</summary>
public sealed record AuthUserDto(long Id, string Email, string FullName, string? OrganizationName)
{
    public static AuthUserDto From(User user, string? organizationName = null) =>
        new(user.Id, user.Email, user.FullName, organizationName);
}
