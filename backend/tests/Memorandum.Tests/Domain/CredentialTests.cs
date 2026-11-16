using Memorandum.Domain.Common;
using Memorandum.Domain.Users;

namespace Memorandum.Tests.Domain;

public sealed class CredentialTests
{
    private static User NewUser() => User.Create("a@b.ru", "Иван", TestHelpers.Now);

    [Fact]
    public void Create_stores_hash_and_links_user()
    {
        var credential = Credential.Create(NewUser(), "hash-1", TestHelpers.Now, "admin");

        Assert.Equal("hash-1", credential.PasswordHash);
        Assert.Equal(TestHelpers.Now, credential.CreatedAt);
        Assert.Equal(TestHelpers.Now, credential.UpdatedAt);
        Assert.Equal("admin", credential.UpdatedBy);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void Create_rejects_blank_hash(string? hash)
    {
        var exception = Assert.Throws<ValidationException>(
            () => Credential.Create(NewUser(), hash, TestHelpers.Now));

        Assert.Equal(ErrorCodes.Required, exception.Code);
    }

    [Fact]
    public void ChangeHash_replaces_hash_and_updates_time()
    {
        var credential = Credential.Create(NewUser(), "hash-1", TestHelpers.Now);

        credential.ChangeHash("hash-2", TestHelpers.Later, "admin");

        Assert.Equal("hash-2", credential.PasswordHash);
        Assert.Equal(TestHelpers.Now, credential.CreatedAt);
        Assert.Equal(TestHelpers.Later, credential.UpdatedAt);
        Assert.Equal("admin", credential.UpdatedBy);
    }

    [Fact]
    public void ChangeHash_rejects_blank_hash()
    {
        var credential = Credential.Create(NewUser(), "hash-1", TestHelpers.Now);

        Assert.Throws<ValidationException>(() => credential.ChangeHash(null, TestHelpers.Later));
    }
}
