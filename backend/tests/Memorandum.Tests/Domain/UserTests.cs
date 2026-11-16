using Memorandum.Domain.Common;
using Memorandum.Domain.Users;

namespace Memorandum.Tests.Domain;

public sealed class UserTests
{
    [Fact]
    public void Create_fills_fields_and_normalizes_email()
    {
        var user = User.Create("  Ivan.Petrov@Example.RU ", "  Иван Петров  ");

        Assert.Equal("Ivan.Petrov@Example.RU", user.Email);
        Assert.Equal("ivan.petrov@example.ru", user.NormalizedEmail);
        Assert.Equal("Иван Петров", user.FullName);
        Assert.True(user.IsActive);
        Assert.Null(user.ActiveOrganizationId);
        Assert.False(string.IsNullOrWhiteSpace(user.SecurityStamp));
    }

    [Fact]
    public void Create_does_not_assign_id()
    {
        Assert.Equal(0, User.Create("a@b.ru", "Иван").Id);
    }

    [Fact]
    public void Create_marks_audit_fields()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var user = User.Create("a@b.ru", "Иван", "admin");
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.InRange(user.CreatedAt, before, after);
        Assert.Equal(user.CreatedAt, user.UpdatedAt);
        Assert.Equal("admin", user.UpdatedBy);
    }

    [Fact]
    public void Create_trims_author()
    {
        Assert.Equal("admin", User.Create("a@b.ru", "Иван", "  admin  ").UpdatedBy);
    }

    [Theory]
    [InlineData(null, "Иван", ErrorCodes.Required)]
    [InlineData("  ", "Иван", ErrorCodes.Required)]
    [InlineData("a@b.ru", null, ErrorCodes.Required)]
    [InlineData("a@b.ru", "  ", ErrorCodes.Required)]
    public void Create_rejects_blank_required_fields(string? email, string? fullName, string expectedCode)
    {
        var exception = Assert.Throws<ValidationException>(() => User.Create(email, fullName));

        Assert.Equal(expectedCode, exception.Code);
    }

    [Fact]
    public void Create_rejects_too_long_email()
    {
        var email = new string('a', User.EmailMaxLength) + "@b.ru";

        var exception = Assert.Throws<ValidationException>(() => User.Create(email, "Иван"));

        Assert.Equal(ErrorCodes.TooLong, exception.Code);
    }

    [Fact]
    public void Rename_changes_name_and_keeps_stamp()
    {
        var user = User.Create("a@b.ru", "Иван");
        var stamp = user.SecurityStamp;

        user.Rename("Пётр", "admin");

        Assert.Equal("Пётр", user.FullName);
        Assert.Equal(stamp, user.SecurityStamp);
        Assert.Equal("admin", user.UpdatedBy);
    }

    [Fact]
    public void Rename_rejects_blank_name()
    {
        var user = User.Create("a@b.ru", "Иван");

        Assert.Throws<ValidationException>(() => user.Rename(null));
    }

    [Fact]
    public void Deactivate_sets_inactive_and_rotates_stamp()
    {
        var user = User.Create("a@b.ru", "Иван");
        var stamp = user.SecurityStamp;

        user.Deactivate();

        Assert.False(user.IsActive);
        Assert.NotEqual(stamp, user.SecurityStamp);
    }

    [Fact]
    public void Activate_restores_access_without_touching_stamp()
    {
        var user = User.Create("a@b.ru", "Иван");
        user.Deactivate();
        var stamp = user.SecurityStamp;

        user.Activate();

        Assert.True(user.IsActive);
        Assert.Equal(stamp, user.SecurityStamp);
    }

    [Fact]
    public void ChangeSecurityStamp_rotates_stamp()
    {
        var user = User.Create("a@b.ru", "Иван");
        var stamp = user.SecurityStamp;

        user.ChangeSecurityStamp();

        Assert.NotEqual(stamp, user.SecurityStamp);
    }

    [Fact]
    public void SetActiveOrganization_stores_id()
    {
        var user = User.Create("a@b.ru", "Иван");

        user.SetActiveOrganization(7);

        Assert.Equal(7, user.ActiveOrganizationId);
    }

    [Fact]
    public void SetActiveOrganization_allows_clearing()
    {
        var user = User.Create("a@b.ru", "Иван");
        user.SetActiveOrganization(7);

        user.SetActiveOrganization(null);

        Assert.Null(user.ActiveOrganizationId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void SetActiveOrganization_rejects_non_positive_id(long organizationId)
    {
        var user = User.Create("a@b.ru", "Иван");

        Assert.Throws<ValidationException>(() => user.SetActiveOrganization(organizationId));
    }
}
