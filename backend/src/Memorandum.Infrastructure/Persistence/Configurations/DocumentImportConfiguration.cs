using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Memorandum.Domain.Documents;

namespace Memorandum.Infrastructure.Persistence.Configurations;

public sealed class DocumentImportConfiguration : IEntityTypeConfiguration<DocumentImport>
{
    public void Configure(EntityTypeBuilder<DocumentImport> builder)
    {
        builder.ToTable("document_imports");
        AuditColumns.Configure(builder);

        builder.Property(import => import.DocumentId)
            .HasColumnName("document_id")
            .IsRequired();

        builder.Property(import => import.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(DocumentImport.FileNameMaxLength);

        builder.Property(import => import.DetectedSchemaCode)
            .HasColumnName("detected_schema_code")
            .HasMaxLength(DocumentImport.SchemaCodeMaxLength);

        builder.Property(import => import.Status)
            .HasColumnName("status")
            .HasMaxLength(16)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(import => import.ImportedFields)
            .HasColumnName("imported_fields")
            .IsRequired();

        // Предупреждения импорта — JSON-массив сообщений.
        builder.Property(import => import.WarningsJson)
            .HasColumnName("warnings_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(import => import.ImportedAt)
            .HasColumnName("imported_at")
            .IsRequired();

        // Исходный файл хранится целиком: импорт без потери данных.
        builder.Property(import => import.OriginalFile)
            .HasColumnName("original_file")
            .HasColumnType("bytea");

        builder.Ignore(import => import.Warnings);

        builder.HasIndex(import => new { import.DocumentId, import.ImportedAt })
            .HasDatabaseName("ix_document_imports_document_id_imported_at");
    }
}
