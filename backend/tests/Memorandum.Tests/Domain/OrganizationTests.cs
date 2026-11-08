using LanguageExt;
using LanguageExt.Common;
using Memorandum.Api.Domain;
using Memorandum.Api.Domain.Organizations;
using Memorandum.Api.Domain.Users;

namespace Memorandum.Tests.Domain;

public sealed class OrganizationTests
{
    private static (int Code, string Message) ErrorOf<T>(Fin<T> fin) =>
        fin.Match(Succ: _ => (0, string.Empty), Fail: e => (e.Code, e.Message));

    private static (int Code, string Message) ErrorOf(Error error) => (error.Code, error.Message);

    private static Organization ValidOrganization() =>
        Organization.Create("ООО Ромашка")
            .Match(Succ: org => org, Fail: e => throw new InvalidOperationException(e.Message));

    private static User ValidUser() =>
        User.Create("oa@example.ru", "Иванов Иван")
            .Match(Succ: user => user, Fail: e => throw new InvalidOperationException(e.Message));

    private static void AddMember(Organization org, long userId, OrganizationRole role) =>
        Assert.True(org.AddMember(userId, role).IsSucc);

    [Fact]
    public void Create_with_empty_name_fails_with_400() =>
        Assert.Equal(ErrorOf(ApiErrors.OrganizationNameRequired), ErrorOf(Organization.Create("   ")));

    [Fact]
    public void AddMember_adds_single_membership_with_role()
    {
        var org = ValidOrganization();

        var result = org.AddMember(1, OrganizationRole.OrgAdmin);

        Assert.True(result.IsSucc);
        var membership = Assert.Single(org.Memberships);
        Assert.Equal(1, membership.UserId);
        Assert.Equal(OrganizationRole.OrgAdmin, membership.Role);
    }

    [Fact]
    public void AddMember_same_user_twice_fails_with_409()
    {
        var org = ValidOrganization();
        AddMember(org, 1, OrganizationRole.Viewer);

        Assert.Equal(ErrorOf(ApiErrors.MemberAlreadyExists), ErrorOf(org.AddMember(1, OrganizationRole.Editor)));
        Assert.Single(org.Memberships);
    }

    [Fact]
    public void Removed_user_can_be_added_again()
    {
        var org = ValidOrganization();
        AddMember(org, 1, OrganizationRole.Viewer);
        Assert.True(org.RemoveMember(1).IsSucc);

        Assert.True(org.AddMember(1, OrganizationRole.Viewer).IsSucc);
        Assert.Single(org.Memberships);
    }

    [Fact]
    public void ChangeRole_updates_role_and_rotates_user_stamp()
    {
        var org = ValidOrganization();
        var user = ValidUser();
        AddMember(org, user.Id, OrganizationRole.Editor);
        var stamp = user.SecurityStamp;

        var result = org.ChangeRole(user, OrganizationRole.OrgAdmin);

        Assert.True(result.IsSucc);
        Assert.Equal(OrganizationRole.OrgAdmin, Assert.Single(org.Memberships).Role);
        Assert.NotEqual(stamp, user.SecurityStamp);
    }

    [Fact]
    public void RemoveMember_last_org_admin_fails_with_409_and_keeps_membership()
    {
        var org = ValidOrganization();
        AddMember(org, 1, OrganizationRole.OrgAdmin);

        Assert.Equal(ErrorOf(ApiErrors.LastOrgAdmin), ErrorOf(org.RemoveMember(1)));
        Assert.Single(org.Memberships);
    }

    [Fact]
    public void RemoveMember_second_of_two_org_admins_fails_with_409()
    {
        var org = ValidOrganization();
        AddMember(org, 1, OrganizationRole.OrgAdmin);
        AddMember(org, 2, OrganizationRole.OrgAdmin);

        Assert.True(org.RemoveMember(1).IsSucc);
        Assert.Equal(ErrorOf(ApiErrors.LastOrgAdmin), ErrorOf(org.RemoveMember(2)));
    }

    [Fact]
    public void RemoveMember_unknown_user_fails_with_404() =>
        Assert.Equal(ErrorOf(ApiErrors.MemberNotFound), ErrorOf(ValidOrganization().RemoveMember(42)));

    [Fact]
    public void Deactivate_stops_adding_members()
    {
        var org = ValidOrganization();

        org.Deactivate();

        Assert.False(org.IsActive);
        Assert.Equal(ErrorOf(ApiErrors.OrganizationInactive), ErrorOf(org.AddMember(1, OrganizationRole.Viewer)));
        Assert.Empty(org.Memberships);
    }
}
