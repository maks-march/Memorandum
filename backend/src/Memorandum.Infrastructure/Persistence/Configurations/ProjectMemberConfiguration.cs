using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Memorandum.Domain.Projects;

namespace Memorandum.Infrastructure.Persistence.Configurations;

public sealed class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("project_members");
        AuditColumns.Configure(builder);

        builder.Property(member => member.ProjectId)
            .HasColumnName("project_id")
            .IsRequired();

        builder.Property(member => member.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(member => member.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.HasOne(member => member.Project)
            .WithMany(project => project.Members)
            .HasForeignKey(member => member.ProjectId)
            .HasConstraintName("fk_project_members_projects")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Domain.Users.User>()
            .WithMany()
            .HasForeignKey(member => member.UserId)
            .HasConstraintName("fk_project_members_users")
            .OnDelete(DeleteBehavior.Cascade);

        // Сотрудник входит в проект один раз; выведенный из проекта не мешает вернуть его снова.
        builder.HasIndex(member => new { member.ProjectId, member.UserId })
            .HasDatabaseName("ux_project_members_project_id_user_id_active")
            .HasFilter("\"is_active\"")
            .IsUnique();
    }
}
