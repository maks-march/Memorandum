using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Memorandum.Domain.Organizations;

namespace Memorandum.Infrastructure.Persistence.Configurations;

public sealed class OrganizationMembershipConfiguration : IEntityTypeConfiguration<OrganizationMembership>
{
    public void Configure(EntityTypeBuilder<OrganizationMembership> builder)
    {
        builder.ToTable("organization_memberships");
        AuditColumns.Configure(builder);

        builder.Property(membership => membership.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(membership => membership.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        // Роль строкой: добавление значения в enum не сдвигает уже сохранённые данные.
        builder.Property(membership => membership.Role)
            .HasColumnName("role")
            .HasMaxLength(16)
            .HasConversion<string>()
            .IsRequired();

        // Должность хранится в участии: она принадлежит паре «сотрудник + организация».
        builder.Property(membership => membership.Position)
            .HasColumnName("position")
            .HasMaxLength(OrganizationMembership.PositionMaxLength);

        builder.Property(membership => membership.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.HasOne(membership => membership.User)
            .WithMany()
            .HasForeignKey(membership => membership.UserId)
            .HasConstraintName("fk_organization_memberships_users")
            .OnDelete(DeleteBehavior.Cascade);

        // Инвариант «пользователь состоит только в одной организации» держит БД:
        // частичный индекс, чтобы исключённые участия не мешали повторному добавлению.
        builder.HasIndex(membership => membership.UserId)
            .HasDatabaseName("ux_organization_memberships_user_id_active")
            .HasFilter("\"is_active\"")
            .IsUnique();
    }
}
