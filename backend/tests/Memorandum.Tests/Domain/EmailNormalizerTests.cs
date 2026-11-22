using Memorandum.Domain.Common;
using Memorandum.Domain.Users;

namespace Memorandum.Tests.Domain;

public sealed class EmailNormalizerTests
{
    [Theory]
    [InlineData("  A@B.RU ", "a@b.ru")]
    [InlineData("Ivan.Petrov@Example.COM", "ivan.petrov@example.com")]
    [InlineData("a@b.ru", "a@b.ru")]
    public void Normalize_trims_and_lowercases(string input, string expected)
    {
        Assert.Equal(expected, EmailNormalizer.Normalize(input));
    }

    [Fact]
    public void Normalize_is_idempotent()
    {
        var once = EmailNormalizer.Normalize("  A@B.RU ");

        Assert.Equal(once, EmailNormalizer.Normalize(once));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Normalize_rejects_blank_value(string? value)
    {
        var exception = Assert.Throws<ValidationException>(() => EmailNormalizer.Normalize(value));

        Assert.Equal(ErrorCodes.Required, exception.Code);
    }
}
