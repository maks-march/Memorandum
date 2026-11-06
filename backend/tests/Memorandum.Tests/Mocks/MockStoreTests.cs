using LanguageExt;
using Memorandum.Api.Mocks;

namespace Memorandum.Tests;

public sealed class MockAuthStoreTests
{
    private static int CodeOf(Fin<MockUser> fin) => fin.Match(Succ: _ => 0, Fail: e => e.Code);

    [Fact]
    public void Authenticate_with_wrong_password_fails_401()
    {
        var store = new MockAuthStore();

        Assert.True(store.Authenticate("oa@example.ru", "wrong").IsFail);
        Assert.Equal(401, CodeOf(store.Authenticate("oa@example.ru", "wrong")));
    }

    [Fact]
    public void Authenticate_with_unknown_email_fails_with_same_code_as_bad_password()
    {
        // одинаковый код намеренно: мок не должен подтверждать существование email
        Assert.Equal(CodeOf(new MockAuthStore().Authenticate("oa@example.ru", "wrong")),
                     CodeOf(new MockAuthStore().Authenticate("nobody@example.ru", "wrong")));
    }

    [Fact]
    public void AddUser_with_duplicate_email_fails_409_without_throwing()
    {
        var store = new MockAuthStore();
        Assert.True(store.AddUser("new@example.ru", "p", "Новый", null).IsSucc);
        Assert.Equal(409, store.AddUser("new@example.ru", "p", "Новый", null).Match(Succ: _ => 0, Fail: e => e.Code));
    }

    [Fact]
    public void RequireUser_without_session_fails_401()
    {
        Assert.Equal(401, new MockAuthStore().RequireUser(null).Match(Succ: _ => 0, Fail: e => e.Code));
        Assert.Equal(401, new MockAuthStore().RequireUser("nope").Match(Succ: _ => 0, Fail: e => e.Code));
    }
}
