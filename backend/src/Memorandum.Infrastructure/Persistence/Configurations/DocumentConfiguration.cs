using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Memorandum.Domain.Documents;

namespace Memorandum.Infrastructure.Persistence.Configurations;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents");
        AuditColumns.Configure(builder);

        builder.Property(document => document.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(document => document.Title)
            .HasColumnName("title")
            .HasMaxLength(Document.TitleMaxLength)
            .IsRequired();

        // Код схемы строкой: схема задаётся справочником, а не значением enum.
        builder.Property(document => document.SchemaCode)
            .HasColumnName("schema_code")
            .HasMaxLength(Document.SchemaCodeMaxLength)
            .IsRequired();

        builder.Property(document => document.Status)
            .HasColumnName("status")
            .HasMaxLength(16)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(document => document.IsArchived)
            .HasColumnName("is_archived")
            .IsRequired();

        builder.Property(document => document.ResponsibleUserId)
            .HasColumnName("responsible_user_id");

        // Результат последней проверки и последний XML — поля документа, а не таблицы истории.
        builder.Property(document => document.CheckedAt)
            .HasColumnName("checked_at");

        builder.Property(document => document.CheckErrorCount)
            .HasColumnName("check_error_count")
            .IsRequired();

        builder.Property(document => document.CheckWarningCount)
            .HasColumnName("check_warning_count")
            .IsRequired();

        // Замечания проверки — JSON-массив: структуру задаёт проверка, а не БД.
        builder.Property(document => document.CheckErrorsJson)
            .HasColumnName("check_errors_json")
            .HasColumnType("jsonb");

        builder.Property(document => document.XmlSha256)
            .HasColumnName("xml_sha256")
            .HasMaxLength(Document.XmlSha256Length);

        builder.Property(document => document.XmlSizeBytes)
            .HasColumnName("xml_size_bytes");

        builder.Property(document => document.XmlValid)
            .HasColumnName("xml_valid");

        builder.HasOne(document => document.Project)
            .WithMany()
            .HasForeignKey(document => document.ProjectId)
            .HasConstraintName("fk_documents_projects")
            .OnDelete(DeleteBehavior.Cascade);

        // Список документов проекта — самый частый запрос.
        builder.HasIndex(document => new { document.ProjectId, document.IsArchived })
            .HasDatabaseName("ix_documents_project_id_is_archived");

        // Экран «Все документы» фильтрует и сортирует по статусу и времени правки.
        builder.HasIndex(document => new { document.Status, document.UpdatedAt })
            .HasDatabaseName("ix_documents_status_updated_at");
    }
}
