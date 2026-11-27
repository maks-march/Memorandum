using Memorandum.Domain.Common;
using Memorandum.Domain.Organizations;
using OrganizationEntity = Memorandum.Domain.Organizations.Organization;
using Memorandum.Domain.Projects;
using Memorandum.Domain.Users;

namespace Memorandum.Tests.Domain;

/// <summary>Правила проекта: архив, участники, ответственный, изоляция организаций.</summary>
public sealed class ProjectTests
{
    private static readonly DateTime Now = new(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);
    private const long OrgId = 10;
    private const long OtherOrgId = 20;

    private static OrganizationEntity NewOrganization() =>
        OrganizationEntity.Create("ООО «Проект»", Now).WithId(OrgId);

    private static Project NewProject(OrganizationEntity? organization = null) =>
        Project.Create(organization ?? NewOrganization(), "Жилой комплекс", "Дом № 1",
            WorkType.Construction, ObjectKind.NonIndustrial, Now);

    [Fact]
    public void Create_fills_fields_and_is_active()
    {
        var project = NewProject();

        Assert.Equal("Жилой комплекс", project.Name);
        Assert.Equal("Дом № 1", project.ObjectName);
        Assert.False(project.IsArchived);
        Assert.Equal(OrgId, project.OrganizationId);
    }

    [Fact]
    public void Create_rejects_empty_name()
    {
        var exception = Assert.Throws<ValidationException>(() =>
            Project.Create(NewOrganization(), " ", "Дом", WorkType.Construction, ObjectKind.NonIndustrial, Now));

        Assert.Equal(ErrorCodes.Required, exception.Code);
    }

    [Fact]
    public void Create_in_inactive_organization_is_conflict()
    {
        var organization = NewOrganization();
        organization.Deactivate(Now);

        var exception = Assert.Throws<ConflictException>(() => NewProject(organization));

        Assert.Equal(ErrorCodes.OrganizationInactive, exception.Code);
    }

    [Fact]
    public void Archived_project_rejects_edits_and_restore_brings_it_back()
    {
        var project = NewProject();
        project.Archive(Now);

        var exception = Assert.Throws<ConflictException>(() =>
            project.Update(NewOrganization(), "Новое", "Дом", WorkType.Construction, ObjectKind.NonIndustrial, Now));
        Assert.Equal(ErrorCodes.ProjectArchived, exception.Code);

        project.Restore(Now);
        project.Update(NewOrganization(), "Новое", "Дом", WorkType.Construction, ObjectKind.NonIndustrial, Now);
        Assert.Equal("Новое", project.Name);
    }

    [Fact]
    public void Member_from_other_organization_is_rejected()
    {
        var project = NewProject();

        var exception = Assert.Throws<ValidationException>(() => project.AddMember(7, OtherOrgId, Now));

        Assert.Equal(ErrorCodes.OrganizationMismatch, exception.Code);
    }

    [Fact]
    public void Member_is_added_once_and_can_be_returned()
    {
        var project = NewProject();

        project.AddMember(7, OrgId, Now);
        Assert.True(project.HasMember(7));

        var duplicate = Assert.Throws<ConflictException>(() => project.AddMember(7, OrgId, Now));
        Assert.Equal(ErrorCodes.MemberAlreadyExists, duplicate.Code);

        project.RemoveMember(7, Now);
        Assert.False(project.HasMember(7));

        project.AddMember(7, OrgId, Now);
        Assert.True(project.HasMember(7));
        Assert.Single(project.Members);
    }

    [Fact]
    public void Responsible_must_be_a_member()
    {
        var project = NewProject();

        var exception = Assert.Throws<ValidationException>(() => project.AssignResponsible(7, Now));
        Assert.Equal(ErrorCodes.ProjectMemberRequired, exception.Code);

        project.AddMember(7, OrgId, Now);
        project.AssignResponsible(7, Now);
        Assert.Equal(7, project.ResponsibleUserId);
    }

    [Fact]
    public void Removed_responsible_is_cleared()
    {
        var project = NewProject();
        project.AddMember(7, OrgId, Now);
        project.AssignResponsible(7, Now);

        project.RemoveMember(7, Now);

        Assert.Null(project.ResponsibleUserId);
    }

    [Fact]
    public void Update_changes_card_fields()
    {
        var project = NewProject();

        project.Update(NewOrganization(), "Новый проект", "Дом № 2",
            WorkType.Reconstruction, ObjectKind.Linear, Now,
            addressText: "  Екатеринбург  ", code: "  123-ПЗ  ",
            regionCode: "66", oktmoCode: "65701000", oktmoName: "Екатеринбург");

        Assert.Equal("Новый проект", project.Name);
        Assert.Equal("Екатеринбург", project.AddressText);
        Assert.Equal("123-ПЗ", project.Code);
        Assert.Equal(WorkType.Reconstruction, project.WorkType);
        Assert.Equal(ObjectKind.Linear, project.ObjectKind);
        Assert.Equal("66", project.RegionCode);
        Assert.Equal("65701000", project.OktmoCode);
    }

    [Fact]
    public void Update_rejects_invalid_region_and_oktmo()
    {
        var project = NewProject();

        var region = Assert.Throws<ValidationException>(() =>
            project.Update(NewOrganization(), "Проект", "Дом", WorkType.Construction, ObjectKind.Linear,
                Now, regionCode: "660"));
        Assert.Equal(ErrorCodes.InvalidFormat, region.Code);

        var oktmo = Assert.Throws<ValidationException>(() =>
            project.Update(NewOrganization(), "Проект", "Дом", WorkType.Construction, ObjectKind.Linear,
                Now, oktmoCode: "123"));
        Assert.Equal(ErrorCodes.InvalidFormat, oktmo.Code);
    }
}
