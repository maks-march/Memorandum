using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Memorandum.Domain.Documents;

namespace Memorandum.Infrastructure.Persistence.Configurations;

public sealed class XmlArtifactConfiguration : IEntityTypeConfiguration<XmlArtifact>
{
    public void Configure(EntityTypeBuilder<XmlArtifact> builder)
    {
        builder.ToTable("xml_artifacts");
        AuditColumns.Configure(builder);

        builder.Property(artifact => artifact.DocumentId)
            .HasColumnName("document_id")
            .IsRequired();

        builder.Property(artifact => artifact.CheckId)
            .HasColumnName("check_id")
            .IsRequired();

        builder.Property(artifact => artifact.GeneratedAt)
            .HasColumnName("generated_at")
            .IsRequired();

        builder.Property(artifact => artifact.Sha256)
            .HasColumnName("sha256")
            .HasMaxLength(XmlArtifact.Sha256Length)
            .IsRequired();

        builder.Property(artifact => artifact.SizeBytes)
            .HasColumnName("size_bytes")
            .IsRequired();

        builder.Ignore(artifact => artifact.SuggestedFileName);

        // Артефакт ссылается на проверку, по которой собран.
        builder.HasOne(artifact => artifact.Check)
            .WithMany()
            .HasForeignKey(artifact => artifact.CheckId)
            .HasConstraintName("fk_xml_artifacts_document_checks")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(artifact => new { artifact.DocumentId, artifact.GeneratedAt })
            .HasDatabaseName("ix_xml_artifacts_document_id_generated_at");
    }
}
