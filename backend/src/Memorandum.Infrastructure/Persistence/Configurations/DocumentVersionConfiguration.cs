using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Memorandum.Domain.Documents;

namespace Memorandum.Infrastructure.Persistence.Configurations;

public sealed class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersion>
{
    public void Configure(EntityTypeBuilder<DocumentVersion> builder)
    {
        builder.ToTable("document_versions");
        AuditColumns.Configure(builder);

        builder.Property(version => version.DocumentId)
            .HasColumnName("document_id")
            .IsRequired();

        builder.Property(version => version.Number)
            .HasColumnName("number")
            .IsRequired();

        // Снапшот содержимого: структура задаётся схемой, поэтому jsonb.
        builder.Property(version => version.DataJson)
            .HasColumnName("data_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(version => version.Comment)
            .HasColumnName("comment")
            .HasMaxLength(DocumentVersion.CommentMaxLength);

        builder.Property(version => version.IsAuto)
            .HasColumnName("is_auto")
            .IsRequired();

        builder.Property(version => version.Status)
            .HasColumnName("status")
            .HasMaxLength(16)
            .HasConversion<string>()
            .IsRequired();

        builder.HasOne(version => version.Document)
            .WithMany(document => document.Versions)
            .HasForeignKey(version => version.DocumentId)
            .HasConstraintName("fk_document_versions_documents")
            .OnDelete(DeleteBehavior.Cascade);

        // Номер версии уникален в документе: история без повторов.
        builder.HasIndex(version => new { version.DocumentId, version.Number })
            .HasDatabaseName("ux_document_versions_document_id_number")
            .IsUnique();
    }
}
