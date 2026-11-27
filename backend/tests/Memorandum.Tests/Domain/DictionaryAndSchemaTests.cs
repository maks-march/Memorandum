using Memorandum.Domain.Common;
using Memorandum.Domain.Dictionaries;
using Memorandum.Domain.Documents;
using Memorandum.Domain.Organizations;
using Memorandum.Domain.Projects;
using Memorandum.Domain.Schemas;
using Memorandum.Domain.Users;
using OrganizationEntity = Memorandum.Domain.Organizations.Organization;

namespace Memorandum.Tests.Domain;

/// <summary>Правила справочников, версий схем и участников документа.</summary>
public sealed class DictionaryAndSchemaTests
{
    [Fact]
    public void Dictionary_element_keeps_code_version_and_is_active()
    {
        var region = Dictionary.Create(
            DictionaryKind.Region, " 66 ", "Свердловская область", "2026.1", TestHelpers.Now, sortOrder: 1);

        Assert.Equal("66", region.Code);
        Assert.Equal(DictionaryKind.Region, region.Kind);
        Assert.Equal("2026.1", region.Version);
        Assert.True(region.IsActive);
        Assert.Null(region.ParentCode);
    }

    [Fact]
    public void Dictionary_element_requires_code_and_name()
    {
        var code = Assert.Throws<ValidationException>(() =>
            Dictionary.Create(DictionaryKind.Unit, " ", "м²", "1", TestHelpers.Now));
        Assert.Equal(ErrorCodes.Required, code.Code);

        var name = Assert.Throws<ValidationException>(() =>
            Dictionary.Create(DictionaryKind.Unit, "m2", "", "1", TestHelpers.Now));
        Assert.Equal(ErrorCodes.Required, name.Code);
    }

    [Fact]
    public void Dictionary_element_can_be_hidden_and_returned()
    {
        var oktmo = Dictionary.Create(
            DictionaryKind.Oktmo, "65701000", "город Екатеринбург", "2026.1", TestHelpers.Now, parentCode: "66");

        oktmo.Deactivate(TestHelpers.Later);
        Assert.False(oktmo.IsActive);

        oktmo.Activate(TestHelpers.Later);
        Assert.True(oktmo.IsActive);
        Assert.Equal(TestHelpers.Later, oktmo.UpdatedAt);
    }

    [Fact]
    public void Package_requires_files_and_computes_hash()
    {
        var version = SchemaVersion.Register("01.07", "Редакция 01.07", TestHelpers.Now, TestHelpers.Now);

        var exception = Assert.Throws<ValidationException>(() =>
            version.ReplacePackage([], TestHelpers.Now));
        Assert.Equal(ErrorCodes.Validation, exception.Code);

        version.ReplacePackage(
            [
                new SchemaPackageFile(SchemaFileKind.Xsd, "explanatorynote-01-07.xsd", "application/xml", [1, 2, 3], TestHelpers.Now),
                new SchemaPackageFile(SchemaFileKind.Xsl, "explanatorynote-01-07.xsl", "application/xml", [4, 5], TestHelpers.Now),
            ],
            TestHelpers.Now);

        Assert.Equal(2, version.PackageFiles.Count);
        Assert.Equal(64, version.Sha256!.Length);
        Assert.False(version.IsRunnable);
    }

    [Fact]
    public void Replacement_keeps_first_package_hash()
    {
        var version = SchemaVersion.Register("01.07", "Редакция 01.07", TestHelpers.Now, TestHelpers.Now);

        version.ReplacePackage(
            [new SchemaPackageFile(SchemaFileKind.Xsd, "main.xsd", "application/xml", [1], TestHelpers.Now)],
            TestHelpers.Now);
        var firstHash = version.Sha256;

        version.ReplacePackage(
            [new SchemaPackageFile(SchemaFileKind.Xsd, "main.xsd", "application/xml", [9, 9], TestHelpers.Now)],
            TestHelpers.Later);

        Assert.Equal(firstHash, version.Sha256);
        Assert.Single(version.PackageFiles);
    }

    [Fact]
    public void Compile_failure_keeps_version_unrunnable()
    {
        var version = SchemaVersion.Register("01.07", "Редакция 01.07", TestHelpers.Now, TestHelpers.Now);
        version.ReplacePackage(
            [new SchemaPackageFile(SchemaFileKind.Xsd, "main.xsd", "application/xml", [1], TestHelpers.Now)],
            TestHelpers.Now);

        version.MarkCompileFailed("main.xsd: элемент «Документ» не объявлен", TestHelpers.Now);

        Assert.False(version.IsRunnable);
        Assert.NotNull(version.CompileMessage);
        Assert.NotNull(version.CompiledAt);

        version.MarkCompiled("main.xsd", "urn:memorandum:note:01.07", TestHelpers.Later);

        Assert.True(version.IsRunnable);
        Assert.Null(version.CompileMessage);
    }

    [Fact]
    public void Deactivated_version_is_not_runnable_and_loses_default()
    {
        var version = SchemaVersion.Register("01.06", "Редакция 01.06", TestHelpers.Now, TestHelpers.Now);
        version.ReplacePackage(
            [new SchemaPackageFile(SchemaFileKind.Xsd, "main.xsd", "application/xml", [1], TestHelpers.Now)],
            TestHelpers.Now);
        version.MarkCompiled("main.xsd", "urn:memorandum:note:01.06", TestHelpers.Now);
        version.MarkDefault(TestHelpers.Now);

        Assert.True(version.IsRunnable);

        version.Deactivate(TestHelpers.Later);

        Assert.False(version.IsRunnable);
        Assert.False(version.IsDefault);

        var exception = Assert.Throws<ConflictException>(() => version.MarkDefault(TestHelpers.Later));
        Assert.Equal(ErrorCodes.SchemaVersionInactive, exception.Code);
    }

    [Fact]
    public void Version_in_use_cannot_be_deleted()
    {
        var version = SchemaVersion.Register("01.07", "Редакция 01.07", TestHelpers.Now, TestHelpers.Now);

        var used = Assert.Throws<ConflictException>(() => version.EnsureDeletable(documentsCount: 3));
        Assert.Equal(ErrorCodes.SchemaInUse, used.Code);

        version.EnsureDeletable(documentsCount: 0);

        version.MarkDefault(TestHelpers.Now);
        var byDefault = Assert.Throws<ConflictException>(() => version.EnsureDeletable(documentsCount: 0));
        Assert.Equal(ErrorCodes.ReferenceInUse, byDefault.Code);
    }

    [Fact]
    public void Foreign_organization_has_no_russian_requisites()
    {
        var organization = OrganizationEntity.Create("ACME GmbH", TestHelpers.Now);

        organization.ChangeDetails(
            "7707083893", "773601001", "1027700132195", "Berlin",
            TestHelpers.Now,
            isForeign: true,
            nrsNopriz: "Не требуется");

        Assert.True(organization.IsForeign);
        Assert.Null(organization.Inn);
        Assert.Null(organization.Kpp);
        Assert.Null(organization.Ogrn);
        Assert.Equal("Не требуется", organization.NrsNopriz);
    }

    [Fact]
    public void Nrs_nopriz_keeps_only_known_formats()
    {
        var organization = OrganizationEntity.Create("ООО «Проект»", TestHelpers.Now);

        organization.ChangeDetails(null, null, null, null, TestHelpers.Now, nrsNopriz: " П-123456 ");
        Assert.Equal("П-123456", organization.NrsNopriz);

        var invalid = Assert.Throws<ValidationException>(() =>
            organization.ChangeDetails(null, null, null, null, TestHelpers.Now, nrsNopriz: "12345"));
        Assert.Equal(ErrorCodes.InvalidFormat, invalid.Code);
    }

    [Fact]
    public void Service_admin_flag_is_set_once()
    {
        var user = User.Create("admin@example.ru", "Админ", TestHelpers.Now);

        user.SetServiceAdmin(true, TestHelpers.Later);
        Assert.True(user.IsServiceAdmin);
        Assert.Equal(TestHelpers.Later, user.UpdatedAt);

        user.SetServiceAdmin(true, TestHelpers.Later.AddHours(1));
        Assert.Equal(TestHelpers.Later, user.UpdatedAt);

        user.SetServiceAdmin(false, TestHelpers.Later.AddHours(2));
        Assert.False(user.IsServiceAdmin);
    }
}

/// <summary>Правила участника документа и снимка объекта.</summary>
public sealed class DocumentParticipantTests
{
    private static Document NewDocument()
    {
        var organization = OrganizationEntity.Create("ООО «Проект»", TestHelpers.Now).WithId(10);

        var project = Project.Create(
                organization, "Проект", "Дом", WorkType.Construction, ObjectKind.NonIndustrial, TestHelpers.Now)
            .WithId(20);

        var schema = SchemaVersion.Register("01.07", "Редакция 01.07", TestHelpers.Now, TestHelpers.Now).WithId(30);

        return Document.Create(project, organization, schema, "Записка", TestHelpers.Now).WithId(1);
    }

    [Fact]
    public void Participant_normalizes_snils_and_nrs()
    {
        var document = NewDocument();

        var gip = document.AddParticipant(
            DocumentParticipantKind.Gip, PartyType.Individual, "  Петров Пётр  ", TestHelpers.Now,
            position: "  ГИП  ", snils: "12345678900", nrsNopriz: "П-123456", order: 2);

        Assert.Equal("Петров Пётр", gip.FullName);
        Assert.Equal("ГИП", gip.Position);
        Assert.Equal("12345678900", gip.Snils);
        Assert.Equal("П-123456", gip.NrsNopriz);
        Assert.Equal(2, gip.Order);
    }

    [Fact]
    public void Participant_requires_name()
    {
        var document = NewDocument();

        var exception = Assert.Throws<ValidationException>(() => document.AddParticipant(
            DocumentParticipantKind.Customer, PartyType.Organization, "  ", TestHelpers.Now));

        Assert.Equal(ErrorCodes.Required, exception.Code);
    }

    [Fact]
    public void Participant_can_be_changed_and_removed()
    {
        var document = NewDocument();
        document.AddParticipant(
            DocumentParticipantKind.Designer, PartyType.Organization, "ООО «Проект»", TestHelpers.Now);

        var designer = Assert.Single(document.Participants);
        designer.Change("ООО «Проект-2»", "Генпроектировщик", null, "И-123456", TestHelpers.Later);

        Assert.Equal("ООО «Проект-2»", designer.FullName);
        Assert.Equal("Генпроектировщик", designer.Position);

        document.RemoveParticipant(DocumentParticipantKind.Designer, TestHelpers.Later);
        Assert.Empty(document.Participants);

        var missing = Assert.Throws<NotFoundException>(() =>
            document.RemoveParticipant(DocumentParticipantKind.Designer, TestHelpers.Later));
        Assert.Equal(ErrorCodes.NotFound, missing.Code);
    }

    [Fact]
    public void Participant_change_is_blocked_in_archived_document()
    {
        var document = NewDocument();
        document.Archive(TestHelpers.Now);

        var exception = Assert.Throws<ConflictException>(() => document.AddParticipant(
            DocumentParticipantKind.Nrs, PartyType.Individual, "Сидоров", TestHelpers.Now));

        Assert.Equal(ErrorCodes.DocumentArchived, exception.Code);
    }

    [Fact]
    public void Snapshot_is_copied_from_project_and_can_be_corrected()
    {
        var organization = OrganizationEntity.Create("ООО «Проект»", TestHelpers.Now).WithId(10);

        var project = Project.Create(
                organization, "Проект", "Дом", WorkType.Reconstruction, ObjectKind.Linear, TestHelpers.Now,
                addressText: "Екатеринбург", regionCode: "66", oktmoCode: "65701000", oktmoName: "город Екатеринбург")
            .WithId(20);

        var schema = SchemaVersion.Register("01.07", "Редакция 01.07", TestHelpers.Now, TestHelpers.Now).WithId(30);
        var document = Document.Create(project, organization, schema, "Записка", TestHelpers.Now);

        document.Object.Change(
            "Дом № 2", WorkType.MajorRepair, ObjectKind.Industrial, "Пермь", "59", "57701000", "город Пермь");

        Assert.Equal("Дом № 2", document.Object.ObjectName);
        Assert.Equal(WorkType.MajorRepair, document.Object.WorkType);
        Assert.Equal("59", document.Object.RegionCode);
        Assert.Equal("57701000", document.Object.OktmoCode);

        // Карточка проекта не меняется: снимок — копия, а не ссылка.
        Assert.Equal("Дом", project.ObjectName);
        Assert.Equal("66", project.RegionCode);
    }

    [Fact]
    public void Snapshot_rejects_invalid_oktmo()
    {
        var document = NewDocument();

        var exception = Assert.Throws<ValidationException>(() => document.Object.Change(
            "Дом", WorkType.Construction, ObjectKind.Linear, null, null, "123", null));

        Assert.Equal(ErrorCodes.InvalidFormat, exception.Code);
    }
}
