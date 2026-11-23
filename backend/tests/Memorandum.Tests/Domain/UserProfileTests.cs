using Memorandum.Domain.Common;
using Memorandum.Domain.Users;

namespace Memorandum.Tests.Domain;

public sealed class UserProfileTests
{
    private static readonly DateTime Now = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Profile_fields_are_trimmed_and_optional_values_cleared()
    {
        var user = User.Create("a@b.ru", "Иван", Now);

        user.ChangeProfile("  Пётр Петров ", "  +7 999 000-00-00 ", Now);
        Assert.Equal("Пётр Петров", user.FullName);
        Assert.Equal("+7 999 000-00-00", user.Phone);

        user.ChangeProfile("Пётр Петров", " ", Now);
        Assert.Null(user.Phone);
    }

    [Fact]
    public void Empty_full_name_is_rejected()
    {
        var user = User.Create("a@b.ru", "Иван", Now);

        var exception = Assert.Throws<ValidationException>(() =>
            user.ChangeProfile("  ", null, Now));

        Assert.Equal(ErrorCodes.Required, exception.Code);
    }
}
