using Memorandum.Domain.Common;

namespace Memorandum.Tests.Domain;

public sealed class GuardTests
{
    [Fact]
    public void Required_trims_value()
    {
        Assert.Equal("Иван", Guard.Required("  Иван  ", "ФИО"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Required_rejects_blank_value(string? value)
    {
        var exception = Assert.Throws<ValidationException>(() => Guard.Required(value, "ФИО"));

        Assert.Equal(ErrorCodes.Required, exception.Code);
    }

    [Fact]
    public void Required_rejects_value_longer_than_limit()
    {
        var exception = Assert.Throws<ValidationException>(() => Guard.Required(new string('a', 11), "ФИО", 10));

        Assert.Equal(ErrorCodes.TooLong, exception.Code);
    }

    [Fact]
    public void Required_accepts_value_of_exact_limit()
    {
        var value = new string('a', 10);

        Assert.Equal(value, Guard.Required(value, "ФИО", 10));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Optional_turns_blank_into_null(string? value)
    {
        Assert.Null(Guard.Optional(value, "Автор"));
    }

    [Fact]
    public void Optional_trims_value()
    {
        Assert.Equal("Пётр", Guard.Optional("  Пётр  ", "Автор"));
    }

    [Fact]
    public void Optional_rejects_value_longer_than_limit()
    {
        Assert.Throws<ValidationException>(() => Guard.Optional(new string('a', 11), "Автор", 10));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PositiveId_rejects_non_positive_value(long value)
    {
        var exception = Assert.Throws<ValidationException>(() => Guard.PositiveId(value, "Организация"));

        Assert.Equal(ErrorCodes.Validation, exception.Code);
    }

    [Fact]
    public void PositiveId_returns_value()
    {
        Assert.Equal(42, Guard.PositiveId(42, "Организация"));
    }
}
