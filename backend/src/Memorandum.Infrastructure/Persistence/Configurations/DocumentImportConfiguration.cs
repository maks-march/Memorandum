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

        builder.Property(import => import.DetectedSchemaCode)
            .HasColumnName("detected_schema_code")
            .HasMaxLength(DocumentImport.SchemaCodeMaxLength);

        builder.Property(import => import.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(DocumentImport.FileNameMaxLength)
            .IsRequired();

        builder.Property(import => import.ImportedFields)
            .HasColumnName("imported_fields")
            .IsRequired();

        // Предупреждения импорта — JSON-массив {path, reason}.
        builder.Property(import => import.WarningsJson)
            .HasColumnName("warnings_json")
            .HasColumnType("jsonb");

        // Исходный файл — поля импорта: ключ в хранилище, размер и хеш.
        builder.Property(import => import.OriginalFileKey)
            .HasColumnName("original_file_key")
            .HasMaxLength(DocumentImport.StorageKeyMaxLength);

        builder.Property(import => import.OriginalFileSizeBytes)
            .HasColumnName("original_file_size_bytes");

        builder.Property(import => import.OriginalFileSha256)
            .HasColumnName("original_file_sha256")
            .HasMaxLength(DocumentImport.Sha256Length);

        builder.HasOne(import => import.Document)
            .WithMany(document => document.Imports)
            .HasForeignKey(import => import.DocumentId)
            .HasConstraintName("fk_document_imports_documents")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(import => import.DocumentId)
            .HasDatabaseName("ix_document_imports_document_id");
    }
}
