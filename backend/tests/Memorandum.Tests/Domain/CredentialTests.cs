using Memorandum.Domain.Common;
using Memorandum.Domain.Users;

namespace Memorandum.Tests.Domain;

public sealed class CredentialTests
{
    [Fact]
    public void Create_stores_hash_and_links_user()
    {
        var user = User.Create("a@b.ru", "Иван");

        var credential = Credential.Create(user, "hash-1", "admin");

        Assert.Equal("hash-1", credential.PasswordHash);
        Assert.Same(user, credential.User);
        Assert.Equal("admin", credential.UpdatedBy);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void Create_rejects_blank_hash(string? hash)
    {
        var user = User.Create("a@b.ru", "Иван");

        var exception = Assert.Throws<ValidationException>(() => Credential.Create(user, hash));

        Assert.Equal(ErrorCodes.Required, exception.Code);
    }

    [Fact]
    public void ChangeHash_replaces_hash()
    {
        var credential = Credential.Create(User.Create("a@b.ru", "Иван"), "hash-1");

        credential.ChangeHash("hash-2", "admin");

        Assert.Equal("hash-2", credential.PasswordHash);
        Assert.Equal("admin", credential.UpdatedBy);
    }

    [Fact]
    public void ChangeHash_rejects_blank_hash()
    {
        var credential = Credential.Create(User.Create("a@b.ru", "Иван"), "hash-1");

        Assert.Throws<ValidationException>(() => credential.ChangeHash(null));
    }
}
