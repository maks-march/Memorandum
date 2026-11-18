using Memorandum.Application.Abstractions;
using Memorandum.Infrastructure.Security;

namespace Memorandum.Tests.Infrastructure;

public sealed class PasswordHasherTests
{
    private readonly AspNetCorePasswordHasher _hasher = new();

    [Fact]
    public void Hash_is_not_equal_to_password()
    {
        Assert.NotEqual("password123", _hasher.Hash("password123"));
    }

    [Fact]
    public void Hash_is_salted_by_random_salt()
    {
        Assert.NotEqual(_hasher.Hash("password123"), _hasher.Hash("password123"));
    }

    [Fact]
    public void Verify_accepts_correct_password()
    {
        var hash = _hasher.Hash("password123");

        Assert.Equal(PasswordVerificationResult.Success, _hasher.Verify(hash, "password123"));
    }

    [Fact]
    public void Verify_rejects_wrong_password()
    {
        var hash = _hasher.Hash("password123");

        Assert.Equal(PasswordVerificationResult.Failed, _hasher.Verify(hash, "password124"));
    }

    [Fact]
    public void Verify_rejects_foreign_hash()
    {
        Assert.Equal(PasswordVerificationResult.Failed, _hasher.Verify("not-a-hash", "password123"));
    }
}
