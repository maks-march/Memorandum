using Memorandum.Domain.Common;
using Memorandum.Domain.Organizations;
using Memorandum.Domain.Users;

namespace Memorandum.Tests.Domain;

public sealed class UserTests
{
    private static User NewUser(string email = "a@b.ru", string fullName = "Иван") =>
        User.Create(email, fullName, TestHelpers.Now);

    [Fact]
    public void Create_fills_fields_and_normalizes_email()
    {
        var user = User.Create("  Ivan.Petrov@Example.RU ", "  Иван Петров  ", TestHelpers.Now);

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
        Assert.Equal(0, NewUser().Id);
    }

    [Fact]
    public void Create_uses_passed_time()
    {
        var user = User.Create("a@b.ru", "Иван", TestHelpers.Now, "admin");

        Assert.Equal(TestHelpers.Now, user.CreatedAt);
        Assert.Equal(TestHelpers.Now, user.UpdatedAt);
        Assert.Equal(DateTimeKind.Utc, user.CreatedAt.Kind);
        Assert.Equal("admin", user.UpdatedBy);
    }

    [Fact]
    public void Create_trims_author()
    {
        Assert.Equal("admin", User.Create("a@b.ru", "Иван", TestHelpers.Now, "  admin  ").UpdatedBy);
    }

    [Theory]
    [InlineData(null, "Иван", ErrorCodes.Required)]
    [InlineData("  ", "Иван", ErrorCodes.Required)]
    [InlineData("a@b.ru", null, ErrorCodes.Required)]
    [InlineData("a@b.ru", "  ", ErrorCodes.Required)]
    public void Create_rejects_blank_required_fields(string? email, string? fullName, string expectedCode)
    {
        var exception = Assert.Throws<ValidationException>(
            () => User.Create(email, fullName, TestHelpers.Now));

        Assert.Equal(expectedCode, exception.Code);
    }

    [Fact]
    public void Create_rejects_too_long_email()
    {
        var email = new string('a', User.EmailMaxLength) + "@b.ru";

        var exception = Assert.Throws<ValidationException>(() => User.Create(email, "Иван", TestHelpers.Now));

        Assert.Equal(ErrorCodes.TooLong, exception.Code);
    }

    [Fact]
    public void Rename_changes_name_and_updates_time()
    {
        var user = NewUser();
        var stamp = user.SecurityStamp;

        user.Rename("Пётр", TestHelpers.Later, "admin");

        Assert.Equal("Пётр", user.FullName);
        Assert.Equal(TestHelpers.Now, user.CreatedAt);
        Assert.Equal(TestHelpers.Later, user.UpdatedAt);
        Assert.Equal("admin", user.UpdatedBy);
        Assert.Equal(stamp, user.SecurityStamp);
    }

    [Fact]
    public void Rename_rejects_blank_name()
    {
        Assert.Throws<ValidationException>(() => NewUser().Rename(null, TestHelpers.Later));
    }

    [Fact]
    public void Rename_keeps_author_when_not_passed()
    {
        var user = User.Create("a@b.ru", "Иван", TestHelpers.Now, "admin");

        user.Rename("Пётр", TestHelpers.Later);

        Assert.Equal("admin", user.UpdatedBy);
    }

    [Fact]
    public void Deactivate_sets_inactive_and_rotates_stamp()
    {
        var user = NewUser();
        var stamp = user.SecurityStamp;

        user.Deactivate(TestHelpers.Later);

        Assert.False(user.IsActive);
        Assert.NotEqual(stamp, user.SecurityStamp);
        Assert.Equal(TestHelpers.Later, user.UpdatedAt);
    }

    [Fact]
    public void Activate_restores_access_without_touching_stamp()
    {
        var user = NewUser();
        user.Deactivate(TestHelpers.Now);
        var stamp = user.SecurityStamp;

        user.Activate(TestHelpers.Later);

        Assert.True(user.IsActive);
        Assert.Equal(stamp, user.SecurityStamp);
        Assert.Equal(TestHelpers.Later, user.UpdatedAt);
    }

    [Fact]
    public void ChangeSecurityStamp_rotates_stamp()
    {
        var user = NewUser();
        var stamp = user.SecurityStamp;

        user.ChangeSecurityStamp(TestHelpers.Later);

        Assert.NotEqual(stamp, user.SecurityStamp);
        Assert.Equal(TestHelpers.Later, user.UpdatedAt);
    }

    [Fact]
    public void SetActiveOrganization_stores_id()
    {
        var user = NewUser();

        user.SetActiveOrganization(7, TestHelpers.Later);

        Assert.Equal(7, user.ActiveOrganizationId);
        Assert.Equal(TestHelpers.Later, user.UpdatedAt);
    }

    [Fact]
    public void SetActiveOrganization_allows_clearing()
    {
        var user = NewUser();
        user.SetActiveOrganization(7, TestHelpers.Now);

        user.SetActiveOrganization((long?)null, TestHelpers.Later);

        Assert.Null(user.ActiveOrganizationId);
        Assert.Equal(TestHelpers.Later, user.UpdatedAt);
    }

    [Fact]
    public void SetActiveOrganization_by_entity_sets_navigation()
    {
        var user = NewUser();
        var organization = Organization.Create("ООО", TestHelpers.Now);

        user.SetActiveOrganization(organization, TestHelpers.Later, "admin");

        Assert.Same(organization, user.ActiveOrganization);
        Assert.Null(user.ActiveOrganizationId);
        Assert.Equal(TestHelpers.Later, user.UpdatedAt);
        Assert.Equal("admin", user.UpdatedBy);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void SetActiveOrganization_rejects_non_positive_id(long organizationId)
    {
        Assert.Throws<ValidationException>(
            () => NewUser().SetActiveOrganization(organizationId, TestHelpers.Later));
    }
}
