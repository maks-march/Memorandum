using Memorandum.Application.Auth;
using Memorandum.Domain.Users;

namespace Memorandum.Tests.Application;

public sealed class ValidatorsTests
{
    private readonly RegisterRequestValidator _register = new();

    [Fact]
    public void Register_accepts_valid_request()
    {
        var result = _register.Validate(new RegisterRequest("a@b.ru", "password123", "Иван", null));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "Email")]
    [InlineData("нет-собаки", "Email")]
    [InlineData("a@b.ru", "Password")]
    [InlineData("a@b.ru", "FullName")]
    public void Register_rejects_invalid_fields(string email, string expectedField)
    {
        var request = new RegisterRequest(
            expectedField == "Email" ? email : "a@b.ru",
            expectedField == "Password" ? "short" : "password123",
            expectedField == "FullName" ? " " : "Иван",
            null);

        var result = _register.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.PropertyName == expectedField);
    }

    [Fact]
    public void Register_rejects_too_long_values()
    {
        var result = _register.Validate(new RegisterRequest(
            new string('a', User.EmailMaxLength) + "@b.ru",
            new string('p', RegisterRequestValidator.PasswordMaxLength + 1),
            new string('и', User.FullNameMaxLength + 1),
            new string('о', 201)));

        Assert.False(result.IsValid);
        Assert.Equal(4, result.Errors.Select(failure => failure.PropertyName).Distinct().Count());
    }

    [Fact]
    public void Register_accepts_boundary_lengths()
    {
        var result = _register.Validate(new RegisterRequest(
            new string('a', User.EmailMaxLength - 6) + "@b.ru",
            new string('p', RegisterRequestValidator.PasswordMaxLength),
            new string('и', User.FullNameMaxLength),
            new string('о', 200)));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Login_requires_email_and_password()
    {
        var result = new LoginRequestValidator().Validate(new LoginRequest(null, null));

        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
    }
}
