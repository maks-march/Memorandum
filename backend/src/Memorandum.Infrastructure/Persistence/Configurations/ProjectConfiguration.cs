using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Memorandum.Domain.Projects;

namespace Memorandum.Infrastructure.Persistence.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("projects");
        AuditColumns.Configure(builder);

        builder.Property(project => project.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.Property(project => project.Name)
            .HasColumnName("name")
            .HasMaxLength(Project.NameMaxLength)
            .IsRequired();

        builder.Property(project => project.ObjectName)
            .HasColumnName("object_name")
            .HasMaxLength(Project.ObjectNameMaxLength)
            .IsRequired();

        builder.Property(project => project.Code)
            .HasColumnName("code")
            .HasMaxLength(Project.CodeMaxLength);

        builder.Property(project => project.AddressText)
            .HasColumnName("address_text")
            .HasMaxLength(Project.AddressMaxLength);

        builder.Property(project => project.ResponsibleUserId)
            .HasColumnName("responsible_user_id");

        builder.Property(project => project.IsArchived)
            .HasColumnName("is_archived")
            .IsRequired();

        builder.HasOne(project => project.Organization)
            .WithMany()
            .HasForeignKey(project => project.OrganizationId)
            .HasConstraintName("fk_projects_organizations")
            .OnDelete(DeleteBehavior.Cascade);

        // Ответственного удалять из users нельзя, пока он ведёт проект.
        builder.HasOne<Domain.Users.User>()
            .WithMany()
            .HasForeignKey(project => project.ResponsibleUserId)
            .HasConstraintName("fk_projects_responsible_user")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(project => project.Members)
            .WithOne(member => member.Project)
            .HasForeignKey(member => member.ProjectId)
            .HasConstraintName("fk_project_members_projects")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(project => project.Members)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Список проектов организации: фильтр по архиву — самый частый.
        builder.HasIndex(project => new { project.OrganizationId, project.IsArchived })
            .HasDatabaseName("ix_projects_organization_id_is_archived");
    }
}
