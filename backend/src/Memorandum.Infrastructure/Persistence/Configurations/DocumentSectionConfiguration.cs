using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Memorandum.Domain.Documents;

namespace Memorandum.Infrastructure.Persistence.Configurations;

public sealed class DocumentSectionConfiguration : IEntityTypeConfiguration<DocumentSection>
{
    public void Configure(EntityTypeBuilder<DocumentSection> builder)
    {
        builder.ToTable("document_sections");
        AuditColumns.Configure(builder);

        builder.Property(section => section.DocumentId)
            .HasColumnName("document_id")
            .IsRequired();

        builder.Property(section => section.Code)
            .HasColumnName("code")
            .HasMaxLength(DocumentSection.CodeMaxLength)
            .IsRequired();

        builder.Property(section => section.Title)
            .HasColumnName("title")
            .HasMaxLength(DocumentSection.TitleMaxLength)
            .IsRequired();

        builder.Property(section => section.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();

        // Содержимое раздела — JSON схемы; структура полей задаётся схемой, не БД.
        builder.Property(section => section.DataJson)
            .HasColumnName("data_json")
            .HasColumnType("jsonb");

        builder.HasOne(section => section.Document)
            .WithMany(document => document.Sections)
            .HasForeignKey(section => section.DocumentId)
            .HasConstraintName("fk_document_sections_documents")
            .OnDelete(DeleteBehavior.Cascade);

        // Раздел в документе один на код; порядок — отдельная колонка.
        builder.HasIndex(section => new { section.DocumentId, section.Code })
            .HasDatabaseName("ux_document_sections_document_id_code")
            .IsUnique();
    }
}
