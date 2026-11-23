using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Memorandum.Domain.Projects;

namespace Memorandum.Infrastructure.Persistence.Configurations;

public sealed class ProjectDocumentConfiguration : IEntityTypeConfiguration<ProjectDocument>
{
    public void Configure(EntityTypeBuilder<ProjectDocument> builder)
    {
        builder.ToTable("project_documents");
        AuditColumns.Configure(builder);

        builder.Property(document => document.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(document => document.Title)
            .HasColumnName("title")
            .HasMaxLength(ProjectDocument.TitleMaxLength)
            .IsRequired();

        // Код схемы строкой: схема задаётся справочником, а не значением enum.
        builder.Property(document => document.SchemaCode)
            .HasColumnName("schema_code")
            .HasMaxLength(ProjectDocument.SchemaCodeMaxLength)
            .IsRequired();

        builder.Property(document => document.Status)
            .HasColumnName("status")
            .HasMaxLength(16)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(document => document.IsArchived)
            .HasColumnName("is_archived")
            .IsRequired();

        builder.HasOne(document => document.Project)
            .WithMany()
            .HasForeignKey(document => document.ProjectId)
            .HasConstraintName("fk_project_documents_projects")
            .OnDelete(DeleteBehavior.Cascade);

        // Список документов проекта — самый частый запрос.
        builder.HasIndex(document => new { document.ProjectId, document.IsArchived })
            .HasDatabaseName("ix_project_documents_project_id_is_archived");
    }
}
