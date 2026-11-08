using Memorandum.Api.Domain;

namespace Memorandum.Tests.Domain;

public sealed class EmailNormalizerTests
{
    [Fact]
    public void Normalize_trims_and_lowercases() =>
        Assert.Equal("oa@example.ru", EmailNormalizer.Normalize(" OA@Example.RU "));

    [Fact]
    public void Normalize_null_throws() =>
        Assert.Throws<ArgumentNullException>(() => EmailNormalizer.Normalize(null!));

    [Fact]
    public void Normalize_empty_throws() =>
        Assert.Throws<ArgumentException>(() => EmailNormalizer.Normalize(""));

    [Fact]
    public void Normalize_is_idempotent()
    {
        var once = EmailNormalizer.Normalize(" OA@Example.RU ");
        Assert.Equal(once, EmailNormalizer.Normalize(once));
    }
}
