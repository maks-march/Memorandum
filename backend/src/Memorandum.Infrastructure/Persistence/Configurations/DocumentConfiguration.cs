using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Memorandum.Domain.Documents;
using Memorandum.Domain.Organizations;

namespace Memorandum.Infrastructure.Persistence.Configurations;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents");
        AuditColumns.Configure(builder);

        builder.Property(document => document.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.Property(document => document.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(document => document.Title)
            .HasColumnName("title")
            .HasMaxLength(Document.TitleMaxLength)
            .IsRequired();

        // Версия схемы фиксируется при создании: удалить версию, по которой есть документы, нельзя.
        builder.Property(document => document.SchemaVersionId)
            .HasColumnName("schema_version_id")
            .IsRequired();

        builder.Property(document => document.Status)
            .HasColumnName("status")
            .HasMaxLength(16)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(document => document.DataJson)
            .HasColumnName("data_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(document => document.ResponsibleUserId)
            .HasColumnName("responsible_user_id");

        builder.Property(document => document.DeletedAt)
            .HasColumnName("deleted_at");

        // Статус до архивации: восстановление возвращает именно его.
        builder.Property(document => document.StatusBeforeArchiving)
            .HasColumnName("status_before_archiving")
            .HasMaxLength(16)
            .HasConversion<string>();

        // Снимок объекта лежит в той же таблице: это часть агрегата, а не отдельная сущность.
        builder.OwnsOne(document => document.Object, snapshot =>
        {
            snapshot.Property(value => value.ObjectName)
                .HasColumnName("object_name")
                .HasMaxLength(DocumentObjectSnapshot.NameMaxLength)
                .IsRequired();
            snapshot.Property(value => value.WorkType)
                .HasColumnName("work_type")
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            snapshot.Property(value => value.ObjectKind)
                .HasColumnName("object_kind")
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
            snapshot.Property(value => value.AddressText)
                .HasColumnName("object_address")
                .HasMaxLength(DocumentObjectSnapshot.AddressMaxLength);
            snapshot.Property(value => value.RegionCode)
                .HasColumnName("region_code")
                .HasMaxLength(DocumentObjectSnapshot.RegionCodeMaxLength);
            snapshot.Property(value => value.OktmoCode)
                .HasColumnName("oktmo_code")
                .HasMaxLength(DocumentObjectSnapshot.OktmoCodeMaxLength);
            snapshot.Property(value => value.OktmoName)
                .HasColumnName("oktmo_name")
                .HasMaxLength(DocumentObjectSnapshot.OktmoNameMaxLength);
        });

        builder.HasOne(document => document.Organization)
            .WithMany()
            .HasForeignKey(document => document.OrganizationId)
            .HasConstraintName("fk_documents_organizations")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(document => document.Project)
            .WithMany()
            .HasForeignKey(document => document.ProjectId)
            .HasConstraintName("fk_documents_projects")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(document => document.SchemaVersion)
            .WithMany()
            .HasForeignKey(document => document.SchemaVersionId)
            .HasConstraintName("fk_documents_schema_versions")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Domain.Users.User>()
            .WithMany()
            .HasForeignKey(document => document.ResponsibleUserId)
            .HasConstraintName("fk_documents_responsible_user")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(document => document.Participants)
            .WithOne(participant => participant.Document)
            .HasForeignKey(participant => participant.DocumentId)
            .HasConstraintName("fk_document_participants_documents")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(document => document.Versions)
            .WithOne(version => version.Document)
            .HasForeignKey(version => version.DocumentId)
            .HasConstraintName("fk_document_versions_documents")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(document => document.Checks)
            .WithOne(check => check.Document)
            .HasForeignKey(check => check.DocumentId)
            .HasConstraintName("fk_document_checks_documents")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(document => document.XmlArtifacts)
            .WithOne(artifact => artifact.Document)
            .HasForeignKey(artifact => artifact.DocumentId)
            .HasConstraintName("fk_xml_artifacts_documents")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(document => document.Imports)
            .WithOne(import => import.Document)
            .HasForeignKey(import => import.DocumentId)
            .HasConstraintName("fk_document_imports_documents")
            .OnDelete(DeleteBehavior.Cascade);

        // Вычисляемые свойства: EF их не хранит.
        builder.Ignore(document => document.LastCheck);
        builder.Ignore(document => document.CanDownloadXml);
        builder.Ignore(document => document.IsArchived);
        builder.Ignore(document => document.IsDeleted);

        builder.Navigation(document => document.Participants).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(document => document.Versions).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(document => document.Checks).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(document => document.XmlArtifacts).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(document => document.Imports).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Список документов проекта и организации — самые частые запросы.
        builder.HasIndex(document => new { document.ProjectId, document.Status })
            .HasDatabaseName("ix_documents_project_id_status");

        builder.HasIndex(document => new { document.OrganizationId, document.DeletedAt })
            .HasDatabaseName("ix_documents_organization_id_deleted_at");

        builder.HasIndex(document => new { document.Status, document.UpdatedAt })
            .HasDatabaseName("ix_documents_status_updated_at");
    }
}
