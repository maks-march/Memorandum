using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Memorandum.Domain.Documents;

namespace Memorandum.Infrastructure.Persistence.Configurations;

public sealed class DocumentCheckConfiguration : IEntityTypeConfiguration<DocumentCheck>
{
    public void Configure(EntityTypeBuilder<DocumentCheck> builder)
    {
        builder.ToTable("document_checks");
        AuditColumns.Configure(builder);

        builder.Property(check => check.DocumentId)
            .HasColumnName("document_id")
            .IsRequired();

        builder.Property(check => check.SchemaVersionId)
            .HasColumnName("schema_version_id")
            .IsRequired();

        builder.Property(check => check.CheckedAt)
            .HasColumnName("checked_at")
            .IsRequired();

        builder.Property(check => check.Ok)
            .HasColumnName("ok")
            .IsRequired();

        builder.Property(check => check.ErrorCount)
            .HasColumnName("error_count")
            .IsRequired();

        builder.Property(check => check.WarningCount)
            .HasColumnName("warning_count")
            .IsRequired();

        // Замечания проверки: структуру задаёт валидатор, поэтому jsonb.
        builder.Property(check => check.IssuesJson)
            .HasColumnName("issues_json")
            .HasColumnType("jsonb")
            .IsRequired();

        // Разобранные замечания — вычисляемое свойство, в БД только JSON.
        builder.Ignore(check => check.Issues);

        builder.HasOne(check => check.SchemaVersion)
            .WithMany()
            .HasForeignKey(check => check.SchemaVersionId)
            .HasConstraintName("fk_document_checks_schema_versions")
            .OnDelete(DeleteBehavior.Restrict);

        // История проверок читается от свежих к старым.
        builder.HasIndex(check => new { check.DocumentId, check.CheckedAt })
            .HasDatabaseName("ix_document_checks_document_id_checked_at");
    }
}
