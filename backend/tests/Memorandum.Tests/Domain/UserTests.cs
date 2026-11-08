using LanguageExt;
using LanguageExt.Common;
using Memorandum.Api.Domain;
using Memorandum.Api.Domain.Users;

namespace Memorandum.Tests.Domain;

public sealed class UserTests
{
    private static (int Code, string Message) ErrorOf<T>(Fin<T> fin) =>
        fin.Match(Succ: _ => (0, string.Empty), Fail: e => (e.Code, e.Message));

    private static (int Code, string Message) ErrorOf(Error error) => (error.Code, error.Message);

    private static User ValidUser() =>
        User.Create("OA@Example.RU", "Иванов Иван", "Инженер-проектировщик")
            .Match(Succ: user => user, Fail: e => throw new InvalidOperationException(e.Message));

    [Fact]
    public void Create_with_valid_data_sets_defaults()
    {
        var user = ValidUser();

        Assert.Equal(0, user.Id);
        Assert.True(user.IsActive);
        Assert.False(string.IsNullOrWhiteSpace(user.SecurityStamp));
    }

    [Fact]
    public void Create_with_blank_full_name_fails_with_400()
    {
        Assert.True(User.Create("oa@example.ru", "   ").IsFail);
        Assert.Equal(ErrorOf(ApiErrors.FullNameRequired), ErrorOf(User.Create("oa@example.ru", "   ")));
    }

    [Fact]
    public void Create_without_at_sign_fails_with_400() =>
        Assert.Equal(ErrorOf(ApiErrors.InvalidEmail), ErrorOf(User.Create("not-an-email", "Иванов Иван")));

    [Fact]
    public void Create_with_empty_email_fails_with_400() =>
        Assert.Equal(ErrorOf(ApiErrors.EmailRequired), ErrorOf(User.Create("", "Иванов Иван")));

    [Fact]
    public void Create_keeps_entered_email_and_stores_normalized_copy()
    {
        var user = ValidUser();

        Assert.Equal("OA@Example.RU", user.Email);
        Assert.Equal("oa@example.ru", user.NormalizedEmail);
    }

    [Fact]
    public void Deactivate_clears_active_and_rotates_security_stamp()
    {
        var user = ValidUser();
        var stamp = user.SecurityStamp;

        user.Deactivate();

        Assert.False(user.IsActive);
        Assert.NotEqual(stamp, user.SecurityStamp);
    }

    [Fact]
    public void ChangeProfile_does_not_rotate_security_stamp()
    {
        var user = ValidUser();
        var stamp = user.SecurityStamp;

        var result = user.ChangeProfile("Петров Пётр", "ГИП");

        Assert.True(result.IsSucc);
        Assert.Equal("Петров Пётр", user.FullName);
        Assert.Equal("ГИП", user.Position);
        Assert.Equal(stamp, user.SecurityStamp);
    }

    [Fact]
    public void IsServiceAdmin_is_false_by_default_and_not_settable_via_factory()
    {
        var user = ValidUser();

        Assert.False(user.IsServiceAdmin);
        Assert.DoesNotContain(typeof(User).GetMethod(nameof(User.Create))!.GetParameters(),
            p => p.Name == "isServiceAdmin");
    }
}
