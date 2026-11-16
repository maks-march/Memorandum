using Memorandum.Domain.Common;
using Memorandum.Domain.Organizations;
using Memorandum.Domain.Users;

namespace Memorandum.Tests.Domain;

public sealed class OrganizationTests
{
    private static User NewUser(long id, string email = "a@b.ru") => User.Create(email, "Иван").WithId(id);

    [Fact]
    public void Create_trims_name_and_activates()
    {
        var organization = Organization.Create("  ООО Ромашка  ", "admin");

        Assert.Equal("ООО Ромашка", organization.Name);
        Assert.True(organization.IsActive);
        Assert.Equal("admin", organization.UpdatedBy);
        Assert.Empty(organization.Memberships);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Create_rejects_blank_name(string? name)
    {
        var exception = Assert.Throws<ValidationException>(() => Organization.Create(name));

        Assert.Equal(ErrorCodes.Required, exception.Code);
    }

    [Fact]
    public void Create_rejects_too_long_name()
    {
        Assert.Throws<ValidationException>(() => Organization.Create(new string('a', Organization.NameMaxLength + 1)));
    }

    [Fact]
    public void Rename_changes_name()
    {
        var organization = Organization.Create("Старое");

        organization.Rename("Новое", "admin");

        Assert.Equal("Новое", organization.Name);
        Assert.Equal("admin", organization.UpdatedBy);
    }

    [Fact]
    public void AddMember_adds_active_membership()
    {
        var organization = Organization.Create("ООО");
        var user = NewUser(1);

        var membership = organization.AddMember(user, OrganizationRole.OrgAdmin, "admin");

        Assert.Single(organization.Memberships);
        Assert.Same(membership, Assert.Single(organization.Memberships));
        Assert.Same(user, membership.User);
        Assert.Equal(OrganizationRole.OrgAdmin, membership.Role);
        Assert.True(membership.IsActive);
        Assert.Equal("admin", membership.UpdatedBy);
    }

    [Fact]
    public void AddMember_rejects_duplicate_active_member()
    {
        var organization = Organization.Create("ООО");
        var user = NewUser(1);
        organization.AddMember(user, OrganizationRole.Editor);

        var exception = Assert.Throws<ConflictException>(() => organization.AddMember(user, OrganizationRole.Viewer));

        Assert.Equal(ErrorCodes.MemberAlreadyExists, exception.Code);
        Assert.Single(organization.Memberships);
    }

    [Fact]
    public void AddMember_rejects_null_user()
    {
        var organization = Organization.Create("ООО");

        Assert.Throws<ArgumentNullException>(() => organization.AddMember(null!, OrganizationRole.Editor));
    }

    [Fact]
    public void AddMember_rejects_inactive_organization()
    {
        var organization = Organization.Create("ООО");
        organization.Deactivate();

        var exception = Assert.Throws<ConflictException>(
            () => organization.AddMember(NewUser(1), OrganizationRole.Editor));

        Assert.Equal(ErrorCodes.OrganizationInactive, exception.Code);
    }

    [Fact]
    public void AddMember_after_removal_reactivates_same_membership()
    {
        var organization = Organization.Create("ООО");
        var user = NewUser(1);
        var first = organization.AddMember(user, OrganizationRole.Editor);
        organization.RemoveMember(user.Id);
        organization.AddMember(user, OrganizationRole.Viewer);

        var membership = Assert.Single(organization.Memberships);
        Assert.Same(first, membership);
        Assert.True(membership.IsActive);
        Assert.Equal(OrganizationRole.Viewer, membership.Role);
    }

    [Fact]
    public void RemoveMember_deactivates_membership()
    {
        var organization = Organization.Create("ООО");
        var user = NewUser(1);
        organization.AddMember(user, OrganizationRole.Editor);

        organization.RemoveMember(user.Id, "admin");

        Assert.False(Assert.Single(organization.Memberships).IsActive);
        Assert.Equal("admin", Assert.Single(organization.Memberships).UpdatedBy);
    }

    [Fact]
    public void RemoveMember_rejects_unknown_user()
    {
        var organization = Organization.Create("ООО");

        var exception = Assert.Throws<NotFoundException>(() => organization.RemoveMember(42));

        Assert.Equal(ErrorCodes.NotFound, exception.Code);
    }

    [Fact]
    public void RemoveMember_rejects_last_admin()
    {
        var organization = Organization.Create("ООО");
        var admin = NewUser(1, "admin@b.ru");
        organization.AddMember(admin, OrganizationRole.OrgAdmin);

        var exception = Assert.Throws<ConflictException>(() => organization.RemoveMember(admin.Id));

        Assert.Equal(ErrorCodes.LastOrgAdminRequired, exception.Code);
        Assert.True(Assert.Single(organization.Memberships).IsActive);
    }

    [Fact]
    public void RemoveMember_allows_admin_when_another_admin_exists()
    {
        var organization = Organization.Create("ООО");
        var first = NewUser(1, "first@b.ru");
        var second = NewUser(2, "second@b.ru");
        organization.AddMember(first, OrganizationRole.OrgAdmin);
        organization.AddMember(second, OrganizationRole.OrgAdmin);

        organization.RemoveMember(first.Id);

        Assert.False(organization.Memberships.Single(m => m.UserId == first.Id).IsActive);
        Assert.True(organization.Memberships.Single(m => m.UserId == second.Id).IsActive);
    }

    [Fact]
    public void ChangeRole_changes_role()
    {
        var organization = Organization.Create("ООО");
        var user = NewUser(1);
        organization.AddMember(user, OrganizationRole.Viewer);

        organization.ChangeRole(user.Id, OrganizationRole.Editor, "admin");

        var membership = Assert.Single(organization.Memberships);
        Assert.Equal(OrganizationRole.Editor, membership.Role);
        Assert.Equal("admin", membership.UpdatedBy);
    }

    [Fact]
    public void ChangeRole_ignores_same_role()
    {
        var organization = Organization.Create("ООО");
        var user = NewUser(1);
        organization.AddMember(user, OrganizationRole.Editor);

        organization.ChangeRole(user.Id, OrganizationRole.Editor, "admin");

        Assert.Null(Assert.Single(organization.Memberships).UpdatedBy);
        Assert.Null(organization.UpdatedBy);
    }

    [Fact]
    public void ChangeRole_rejects_demoting_last_admin()
    {
        var organization = Organization.Create("ООО");
        var admin = NewUser(1, "admin@b.ru");
        organization.AddMember(admin, OrganizationRole.OrgAdmin);

        var exception = Assert.Throws<ConflictException>(
            () => organization.ChangeRole(admin.Id, OrganizationRole.Viewer));

        Assert.Equal(ErrorCodes.LastOrgAdminRequired, exception.Code);
        Assert.Equal(OrganizationRole.OrgAdmin, Assert.Single(organization.Memberships).Role);
    }

    [Fact]
    public void ChangeRole_allows_demoting_admin_when_another_admin_exists()
    {
        var organization = Organization.Create("ООО");
        var first = NewUser(1, "first@b.ru");
        var second = NewUser(2, "second@b.ru");
        organization.AddMember(first, OrganizationRole.OrgAdmin);
        organization.AddMember(second, OrganizationRole.OrgAdmin);

        organization.ChangeRole(first.Id, OrganizationRole.Editor);

        Assert.Equal(OrganizationRole.Editor, organization.Memberships.Single(m => m.UserId == first.Id).Role);
    }

    [Fact]
    public void ChangeRole_rejects_unknown_user()
    {
        var organization = Organization.Create("ООО");

        Assert.Throws<NotFoundException>(() => organization.ChangeRole(42, OrganizationRole.Editor));
    }

    [Fact]
    public void ChangeRole_ignores_inactive_membership()
    {
        var organization = Organization.Create("ООО");
        var first = NewUser(1, "first@b.ru");
        var second = NewUser(2, "second@b.ru");
        organization.AddMember(first, OrganizationRole.OrgAdmin);
        organization.AddMember(second, OrganizationRole.OrgAdmin);
        organization.RemoveMember(second.Id);

        Assert.Throws<NotFoundException>(() => organization.ChangeRole(second.Id, OrganizationRole.Viewer));
    }

    [Fact]
    public void Deactivate_marks_organization_inactive()
    {
        var organization = Organization.Create("ООО");

        organization.Deactivate("admin");

        Assert.False(organization.IsActive);
        Assert.Equal("admin", organization.UpdatedBy);
    }
}
