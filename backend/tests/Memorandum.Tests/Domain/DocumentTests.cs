using Memorandum.Domain.Common;
using Memorandum.Domain.Documents;
using Memorandum.Domain.Organizations;
using Memorandum.Domain.Projects;
using Memorandum.Domain.Schemas;
using OrganizationEntity = Memorandum.Domain.Organizations.Organization;

namespace Memorandum.Tests.Domain;

/// <summary>Правила документа: схема, правки, проверка, выгрузка XML, архив, версии.</summary>
public sealed class DocumentTests
{
    private const long OrgId = 10;
    private const long ProjectId = 20;
    private const long SchemaVersionId = 30;
    private const long MemberId = 40;

    private static OrganizationEntity NewOrganization() =>
        OrganizationEntity.Create("ООО «Проект»", TestHelpers.Now).WithId(OrgId);

    private static SchemaVersion NewSchema(bool isActive = true)
    {
        var version = SchemaVersion
            .Register("01.07", "Редакция 01.07", TestHelpers.Now, TestHelpers.Now)
            .WithId(SchemaVersionId);

        if (!isActive)
            version.Deactivate(TestHelpers.Now);

        return version;
    }

    private static Project NewProject(OrganizationEntity? organization = null)
    {
        var owner = organization ?? NewOrganization();

        var project = Project.Create(
                owner,
                "Жилой комплекс",
                "Дом № 1",
                WorkType.Construction,
                ObjectKind.NonIndustrial,
                TestHelpers.Now,
                addressText: "г. Екатеринбург",
                regionCode: "66",
                oktmoCode: "65701000",
                oktmoName: "город Екатеринбург")
            .WithId(ProjectId);

        project.AddMember(MemberId, OrgId, TestHelpers.Now);
        return project;
    }

    private static Document NewDocument(
        Project? project = null,
        OrganizationEntity? organization = null,
        SchemaVersion? schema = null) =>
        Document.Create(
            project ?? NewProject(),
            organization ?? NewOrganization(),
            schema ?? NewSchema(),
            "Пояснительная записка",
            TestHelpers.Now).WithId(1);

    [Fact]
    public void Create_copies_object_snapshot_and_starts_as_draft()
    {
        var document = NewDocument();

        Assert.Equal(DocumentStatus.Draft, document.Status);
        Assert.Equal("Дом № 1", document.Object.ObjectName);
        Assert.Equal(WorkType.Construction, document.Object.WorkType);
        Assert.Equal("65701000", document.Object.OktmoCode);
        Assert.Equal("{}", document.DataJson);
        Assert.Equal(OrgId, document.OrganizationId);
        Assert.Equal(ProjectId, document.ProjectId);
        Assert.Equal(SchemaVersionId, document.SchemaVersionId);
    }

    [Fact]
    public void Create_in_archived_project_is_conflict()
    {
        var project = NewProject();
        project.Archive(TestHelpers.Now);

        var exception = Assert.Throws<ConflictException>(() => Document.Create(
            project, NewOrganization(), NewSchema(), "Записка", TestHelpers.Now));

        Assert.Equal(ErrorCodes.ProjectArchived, exception.Code);
    }

    [Fact]
    public void Create_with_inactive_schema_is_conflict()
    {
        var exception = Assert.Throws<ConflictException>(() =>
            NewDocument(schema: NewSchema(isActive: false)));

        Assert.Equal(ErrorCodes.SchemaInactive, exception.Code);
    }

    [Fact]
    public void Create_rejects_empty_title()
    {
        var exception = Assert.Throws<ValidationException>(() => Document.Create(
            NewProject(), NewOrganization(), NewSchema(), " ", TestHelpers.Now));

        Assert.Equal(ErrorCodes.Required, exception.Code);
    }

    [Fact]
    public void Schema_cannot_be_changed()
    {
        var document = NewDocument();
        var otherSchema = SchemaVersion
            .Register("01.06", "Редакция 01.06", TestHelpers.Now, TestHelpers.Now)
            .WithId(SchemaVersionId + 1);

        var exception = Assert.Throws<ConflictException>(() => document.ChangeSchema(otherSchema));

        Assert.Equal(ErrorCodes.DocumentSchemaImmutable, exception.Code);
        document.ChangeSchema(NewSchema());
    }

    [Fact]
    public void Filling_section_keeps_other_sections()
    {
        var document = NewDocument();

        document.FillSection(DocumentSectionCode.Object, """{"objectName":"Дом"}""", TestHelpers.Now);
        document.FillSection(DocumentSectionCode.Tep, """{"rows":[{"code":"area_total","value":1200}]}""", TestHelpers.Now);

        Assert.Equal("""{"objectName":"Дом"}""", document.SectionData(DocumentSectionCode.Object));
        Assert.Contains("area_total", document.SectionData(DocumentSectionCode.Tep));
        Assert.Null(document.SectionData(DocumentSectionCode.Climate));
        Assert.Equal(2, DocumentData.FilledSections(document.DataJson).Count);
    }

    [Fact]
    public void Filling_section_with_invalid_json_is_rejected()
    {
        var document = NewDocument();

        var exception = Assert.Throws<ValidationException>(() =>
            document.FillSection(DocumentSectionCode.Object, "{ не json", TestHelpers.Now));

        Assert.Equal(ErrorCodes.InvalidFormat, exception.Code);
    }

    [Fact]
    public void Edit_reopens_verified_document()
    {
        var document = NewDocument();
        document.RecordCheck(true, [], TestHelpers.Now);
        Assert.Equal(DocumentStatus.Verified, document.Status);
        Assert.True(document.CanDownloadXml);

        document.FillSection(DocumentSectionCode.Tep, """{"rows":[]}""", TestHelpers.Later);

        Assert.Equal(DocumentStatus.Draft, document.Status);
        Assert.False(document.CanDownloadXml);
    }

    [Fact]
    public void Failed_check_sets_has_errors_and_blocks_xml()
    {
        var document = NewDocument();

        var check = document.RecordCheck(
            false,
            [
                new DocumentCheckIssue(DocumentIssueSeverity.Error, "REQUIRED", "Не заполнена площадь", DocumentSectionCode.Tep),
                new DocumentCheckIssue(DocumentIssueSeverity.Warning, "TABLE_EMPTY", "Таблица пуста", DocumentSectionCode.Tep),
            ],
            TestHelpers.Now);

        Assert.Equal(DocumentStatus.HasErrors, document.Status);
        Assert.Equal(1, check.ErrorCount);
        Assert.Equal(1, check.WarningCount);
        Assert.Equal(2, check.Issues.Count);

        var exception = Assert.Throws<ConflictException>(() => document.RecordXml(
            check, new string('a', 64), 100, TestHelpers.Now));
        Assert.Equal(ErrorCodes.DocumentNotVerified, exception.Code);
    }

    [Fact]
    public void Successful_check_allows_xml_artifact()
    {
        var document = NewDocument();

        var check = document.RecordCheck(true, [], TestHelpers.Now);
        var artifact = document.RecordXml(check, new string('b', 64), 2048, TestHelpers.Now);

        Assert.Equal(DocumentStatus.Verified, document.Status);
        Assert.Equal(2048, artifact.SizeBytes);
        Assert.Single(document.XmlArtifacts);
        Assert.True(document.CanDownloadXml);
    }

    [Fact]
    public void Xml_requires_successful_last_check()
    {
        var document = NewDocument();
        var okCheck = document.RecordCheck(true, [], TestHelpers.Now);
        var failedCheck = document.RecordCheck(
            false,
            [new DocumentCheckIssue(DocumentIssueSeverity.Error, "REQUIRED", "Не заполнено")],
            TestHelpers.Later);

        var stale = Assert.Throws<ConflictException>(() =>
            document.RecordXml(okCheck, new string('c', 64), 10, TestHelpers.Later));
        Assert.Equal(ErrorCodes.StaleVersion, stale.Code);

        var notVerified = Assert.Throws<ConflictException>(() =>
            document.RecordXml(failedCheck, new string('c', 64), 10, TestHelpers.Later));
        Assert.Equal(ErrorCodes.DocumentNotVerified, notVerified.Code);
    }

    [Fact]
    public void Check_with_errors_cannot_be_successful()
    {
        var document = NewDocument();

        var exception = Assert.Throws<ValidationException>(() => document.RecordCheck(
            true,
            [new DocumentCheckIssue(DocumentIssueSeverity.Error, "REQUIRED", "Не заполнено")],
            TestHelpers.Now));

        Assert.Equal(ErrorCodes.ValidationFailed, exception.Code);
    }

    [Fact]
    public void Only_one_gip_allowed()
    {
        var document = NewDocument();

        document.AddParticipant(
            DocumentParticipantKind.Gip, PartyType.Individual, "Петров Пётр", TestHelpers.Now,
            position: "ГИП", nrsNopriz: "П-123456");

        var exception = Assert.Throws<ConflictException>(() => document.AddParticipant(
            DocumentParticipantKind.Gip, PartyType.Individual, "Сидоров", TestHelpers.Now));

        Assert.Equal(ErrorCodes.ParticipantDuplicateGip, exception.Code);
        Assert.Single(document.Participants);
    }

    [Fact]
    public void Participant_rejects_bad_snils_and_nrs()
    {
        var document = NewDocument();

        var gip = document.AddParticipant(
            DocumentParticipantKind.Gip, PartyType.Individual, "Петров Пётр", TestHelpers.Now);

        var snils = Assert.Throws<ValidationException>(() =>
            gip.Change("Петров Пётр", "ГИП", "12-34", "П-123456", TestHelpers.Now));
        Assert.Equal(ErrorCodes.InvalidFormat, snils.Code);

        var nrs = Assert.Throws<ValidationException>(() =>
            gip.Change("Петров Пётр", "ГИП", "123-456-789 00", "АБВ", TestHelpers.Now));
        Assert.Equal(ErrorCodes.InvalidFormat, nrs.Code);
    }

    [Fact]
    public void Archived_document_is_read_only()
    {
        var document = NewDocument();
        document.RecordCheck(true, [], TestHelpers.Now);

        document.Archive(TestHelpers.Now);
        Assert.Equal(DocumentStatus.Archived, document.Status);

        var edit = Assert.Throws<ConflictException>(() =>
            document.FillSection(DocumentSectionCode.Object, "{}", TestHelpers.Now));
        Assert.Equal(ErrorCodes.DocumentArchived, edit.Code);

        var rename = Assert.Throws<ConflictException>(() => document.Rename("Новое", TestHelpers.Now));
        Assert.Equal(ErrorCodes.DocumentArchived, rename.Code);

        var check = Assert.Throws<ConflictException>(() => document.RecordCheck(true, [], TestHelpers.Now));
        Assert.Equal(ErrorCodes.DocumentArchived, check.Code);
    }

    [Fact]
    public void Unarchive_returns_previous_status()
    {
        var document = NewDocument();
        document.RecordCheck(true, [], TestHelpers.Now);
        document.Archive(TestHelpers.Now);

        document.Unarchive(TestHelpers.Now);

        Assert.Equal(DocumentStatus.Verified, document.Status);
    }

    [Fact]
    public void Versions_are_numbered_and_restore_snapshots_current_state()
    {
        var document = NewDocument();
        document.FillSection(DocumentSectionCode.Object, """{"objectName":"Дом"}""", TestHelpers.Now);

        var first = document.AddVersion(TestHelpers.Now, "Первая редакция");
        Assert.Equal(1, first.Number);

        document.FillSection(DocumentSectionCode.Object, """{"objectName":"Дом № 2"}""", TestHelpers.Later);

        var auto = document.RestoreVersion(1, TestHelpers.Later);

        Assert.Equal(2, auto.Number);
        Assert.True(auto.IsAuto);
        Assert.Equal("""{"objectName":"Дом"}""", document.SectionData(DocumentSectionCode.Object));
        Assert.Equal(DocumentStatus.Draft, document.Status);
        Assert.Contains("Дом № 2", auto.DataJson);
    }

    [Fact]
    public void Missing_version_is_not_found()
    {
        var document = NewDocument();

        var exception = Assert.Throws<NotFoundException>(() => document.RestoreVersion(7, TestHelpers.Now));

        Assert.Equal(ErrorCodes.NotFound, exception.Code);
    }

    [Fact]
    public void Soft_delete_keeps_versions_and_checks()
    {
        var document = NewDocument();
        document.FillSection(DocumentSectionCode.Object, "{}", TestHelpers.Now);
        document.AddVersion(TestHelpers.Now);
        document.RecordCheck(true, [], TestHelpers.Now);

        document.Delete(TestHelpers.Now);

        Assert.True(document.IsDeleted);
        Assert.Single(document.Versions);
        Assert.Single(document.Checks);

        document.Restore(TestHelpers.Now);
        Assert.False(document.IsDeleted);
    }

    [Fact]
    public void Responsible_must_be_project_member()
    {
        var document = NewDocument();

        var exception = Assert.Throws<ValidationException>(() =>
            document.AssignResponsible(MemberId + 1, TestHelpers.Now));
        Assert.Equal(ErrorCodes.ProjectMemberRequired, exception.Code);

        document.AssignResponsible(MemberId, TestHelpers.Now);
        Assert.Equal(MemberId, document.ResponsibleUserId);

        document.AssignResponsible(null, TestHelpers.Now);
        Assert.Null(document.ResponsibleUserId);
    }

    [Fact]
    public void Import_is_recorded_with_original_file_and_warnings()
    {
        var document = NewDocument();

        var import = document.RecordImport(
            ImportStatus.Partial,
            "zapiska07.xml",
            "01.07",
            importedFields: 118,
            TestHelpers.Now,
            warnings: ["СведенияОбОбъекте/КадастровыйНомер: поле не найдено"],
            originalFile: [1, 2, 3]);

        Assert.Equal(ImportStatus.Partial, import.Status);
        Assert.Equal(118, import.ImportedFields);
        Assert.Single(import.Warnings);
        Assert.Equal(3, import.OriginalFile!.Length);

        var failed = document.RecordImport(
            ImportStatus.Failed, "unknown.xml", "01.05", importedFields: 0, TestHelpers.Later,
            warnings: ["Версия схемы не поддерживается"]);

        Assert.Equal(ImportStatus.Failed, failed.Status);
        Assert.Equal(2, document.Imports.Count);
    }
}
