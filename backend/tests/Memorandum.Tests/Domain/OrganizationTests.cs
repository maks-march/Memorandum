using Memorandum.Domain.Common;
using Memorandum.Domain.Organizations;
using Memorandum.Domain.Users;

namespace Memorandum.Tests.Domain;

public sealed class OrganizationTests
{
    private static User NewUser(long id, string email = "a@b.ru") =>
        User.Create(email, "Иван", TestHelpers.Now).WithId(id);

    private static Organization NewOrganization(string name = "ООО") =>
        Organization.Create(name, TestHelpers.Now);

    [Fact]
    public void Create_trims_name_and_activates()
    {
        var organization = Organization.Create("  ООО Ромашка  ", TestHelpers.Now, "admin");

        Assert.Equal("ООО Ромашка", organization.Name);
        Assert.True(organization.IsActive);
        Assert.Equal("admin", organization.UpdatedBy);
        Assert.Empty(organization.Memberships);
    }

    [Fact]
    public void Create_uses_passed_time()
    {
        var organization = NewOrganization();

        Assert.Equal(TestHelpers.Now, organization.CreatedAt);
        Assert.Equal(TestHelpers.Now, organization.UpdatedAt);
        Assert.Equal(DateTimeKind.Utc, organization.CreatedAt.Kind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Create_rejects_blank_name(string? name)
    {
        var exception = Assert.Throws<ValidationException>(
            () => Organization.Create(name, TestHelpers.Now));

        Assert.Equal(ErrorCodes.Required, exception.Code);
    }

    [Fact]
    public void Create_rejects_too_long_name()
    {
        var name = new string('a', Organization.NameMaxLength + 1);

        Assert.Throws<ValidationException>(() => Organization.Create(name, TestHelpers.Now));
    }

    [Fact]
    public void Rename_changes_name_and_updates_time()
    {
        var organization = NewOrganization("Старое");

        organization.Rename("Новое", TestHelpers.Later, "admin");

        Assert.Equal("Новое", organization.Name);
        Assert.Equal(TestHelpers.Now, organization.CreatedAt);
        Assert.Equal(TestHelpers.Later, organization.UpdatedAt);
        Assert.Equal("admin", organization.UpdatedBy);
    }

    [Fact]
    public void AddMember_adds_active_membership()
    {
        var organization = NewOrganization();
        var user = NewUser(1);

        var membership = organization.AddMember(user, OrganizationRole.OrgAdmin, TestHelpers.Now, "admin");

        Assert.Same(membership, Assert.Single(organization.Memberships));
        Assert.Same(user, membership.User);
        Assert.Equal(OrganizationRole.OrgAdmin, membership.Role);
        Assert.True(membership.IsActive);
        Assert.Equal(TestHelpers.Now, membership.CreatedAt);
        Assert.Equal("admin", membership.UpdatedBy);
    }

    [Fact]
    public void AddMember_rejects_duplicate_active_member()
    {
        var organization = NewOrganization();
        var user = NewUser(1);
        organization.AddMember(user, OrganizationRole.Editor, TestHelpers.Now);

        var exception = Assert.Throws<ConflictException>(
            () => organization.AddMember(user, OrganizationRole.Viewer, TestHelpers.Later));

        Assert.Equal(ErrorCodes.MemberAlreadyExists, exception.Code);
        Assert.Single(organization.Memberships);
    }

    [Fact]
    public void AddMember_rejects_null_user()
    {
        var organization = NewOrganization();

        Assert.Throws<ArgumentNullException>(
            () => organization.AddMember(null!, OrganizationRole.Editor, TestHelpers.Now));
    }

    [Fact]
    public void AddMember_rejects_inactive_organization()
    {
        var organization = NewOrganization();
        organization.Deactivate(TestHelpers.Now);

        var exception = Assert.Throws<ConflictException>(
            () => organization.AddMember(NewUser(1), OrganizationRole.Editor, TestHelpers.Later));

        Assert.Equal(ErrorCodes.OrganizationInactive, exception.Code);
    }

    [Fact]
    public void AddMember_after_removal_reactivates_same_membership()
    {
        var organization = NewOrganization();
        var user = NewUser(1);
        var first = organization.AddMember(user, OrganizationRole.Editor, TestHelpers.Now);
        organization.RemoveMember(user.Id, TestHelpers.Now);
        organization.AddMember(user, OrganizationRole.Viewer, TestHelpers.Later);

        var membership = Assert.Single(organization.Memberships);
        Assert.Same(first, membership);
        Assert.True(membership.IsActive);
        Assert.Equal(OrganizationRole.Viewer, membership.Role);
        Assert.Equal(TestHelpers.Now, membership.CreatedAt);
        Assert.Equal(TestHelpers.Later, membership.UpdatedAt);
    }

    [Fact]
    public void RemoveMember_deactivates_membership()
    {
        var organization = NewOrganization();
        var user = NewUser(1);
        organization.AddMember(user, OrganizationRole.Editor, TestHelpers.Now);

        organization.RemoveMember(user.Id, TestHelpers.Later, "admin");

        var membership = Assert.Single(organization.Memberships);
        Assert.False(membership.IsActive);
        Assert.Equal(TestHelpers.Now, membership.CreatedAt);
        Assert.Equal(TestHelpers.Later, membership.UpdatedAt);
        Assert.Equal("admin", membership.UpdatedBy);
    }

    [Fact]
    public void RemoveMember_rejects_unknown_user()
    {
        var organization = NewOrganization();

        var exception = Assert.Throws<NotFoundException>(
            () => organization.RemoveMember(42, TestHelpers.Later));

        Assert.Equal(ErrorCodes.NotFound, exception.Code);
    }

    [Fact]
    public void RemoveMember_rejects_last_admin()
    {
        var organization = NewOrganization();
        var admin = NewUser(1, "admin@b.ru");
        organization.AddMember(admin, OrganizationRole.OrgAdmin, TestHelpers.Now);

        var exception = Assert.Throws<ConflictException>(
            () => organization.RemoveMember(admin.Id, TestHelpers.Later));

        Assert.Equal(ErrorCodes.LastOrgAdminRequired, exception.Code);
        Assert.True(Assert.Single(organization.Memberships).IsActive);
    }

    [Fact]
    public void RemoveMember_allows_admin_when_another_admin_exists()
    {
        var organization = NewOrganization();
        var first = NewUser(1, "first@b.ru");
        var second = NewUser(2, "second@b.ru");
        organization.AddMember(first, OrganizationRole.OrgAdmin, TestHelpers.Now);
        organization.AddMember(second, OrganizationRole.OrgAdmin, TestHelpers.Now);

        organization.RemoveMember(first.Id, TestHelpers.Later);

        Assert.False(organization.Memberships.Single(m => m.UserId == first.Id).IsActive);
        Assert.True(organization.Memberships.Single(m => m.UserId == second.Id).IsActive);
    }

    [Fact]
    public void ChangeRole_changes_role_and_updates_time()
    {
        var organization = NewOrganization();
        var user = NewUser(1);
        organization.AddMember(user, OrganizationRole.Viewer, TestHelpers.Now);

        organization.ChangeRole(user.Id, OrganizationRole.Editor, TestHelpers.Later, "admin");

        var membership = Assert.Single(organization.Memberships);
        Assert.Equal(OrganizationRole.Editor, membership.Role);
        Assert.Equal(TestHelpers.Now, membership.CreatedAt);
        Assert.Equal(TestHelpers.Later, membership.UpdatedAt);
        Assert.Equal("admin", membership.UpdatedBy);
    }

    [Fact]
    public void ChangeRole_ignores_same_role()
    {
        var organization = NewOrganization();
        var user = NewUser(1);
        organization.AddMember(user, OrganizationRole.Editor, TestHelpers.Now);

        organization.ChangeRole(user.Id, OrganizationRole.Editor, TestHelpers.Later, "admin");

        Assert.Null(Assert.Single(organization.Memberships).UpdatedBy);
        Assert.Equal(TestHelpers.Now, Assert.Single(organization.Memberships).UpdatedAt);
        Assert.Null(organization.UpdatedBy);
    }

    [Fact]
    public void ChangeRole_rejects_demoting_last_admin()
    {
        var organization = NewOrganization();
        var admin = NewUser(1, "admin@b.ru");
        organization.AddMember(admin, OrganizationRole.OrgAdmin, TestHelpers.Now);

        var exception = Assert.Throws<ConflictException>(
            () => organization.ChangeRole(admin.Id, OrganizationRole.Viewer, TestHelpers.Later));

        Assert.Equal(ErrorCodes.LastOrgAdminRequired, exception.Code);
        Assert.Equal(OrganizationRole.OrgAdmin, Assert.Single(organization.Memberships).Role);
    }

    [Fact]
    public void ChangeRole_allows_demoting_admin_when_another_admin_exists()
    {
        var organization = NewOrganization();
        var first = NewUser(1, "first@b.ru");
        var second = NewUser(2, "second@b.ru");
        organization.AddMember(first, OrganizationRole.OrgAdmin, TestHelpers.Now);
        organization.AddMember(second, OrganizationRole.OrgAdmin, TestHelpers.Now);

        organization.ChangeRole(first.Id, OrganizationRole.Editor, TestHelpers.Later);

        Assert.Equal(OrganizationRole.Editor, organization.Memberships.Single(m => m.UserId == first.Id).Role);
    }

    [Fact]
    public void ChangeRole_rejects_unknown_user()
    {
        var organization = NewOrganization();

        Assert.Throws<NotFoundException>(
            () => organization.ChangeRole(42, OrganizationRole.Editor, TestHelpers.Later));
    }

    [Fact]
    public void ChangeRole_ignores_inactive_membership()
    {
        var organization = NewOrganization();
        var first = NewUser(1, "first@b.ru");
        var second = NewUser(2, "second@b.ru");
        organization.AddMember(first, OrganizationRole.OrgAdmin, TestHelpers.Now);
        organization.AddMember(second, OrganizationRole.OrgAdmin, TestHelpers.Now);
        organization.RemoveMember(second.Id, TestHelpers.Now);

        Assert.Throws<NotFoundException>(
            () => organization.ChangeRole(second.Id, OrganizationRole.Viewer, TestHelpers.Later));
    }

    [Fact]
    public void Deactivate_marks_organization_inactive()
    {
        var organization = NewOrganization();

        organization.Deactivate(TestHelpers.Later, "admin");

        Assert.False(organization.IsActive);
        Assert.Equal(TestHelpers.Later, organization.UpdatedAt);
        Assert.Equal("admin", organization.UpdatedBy);
    }
}
