using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Memorandum.Domain.Users;

namespace Memorandum.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        AuditColumns.Configure(builder);

        builder.Property(user => user.Email)
            .HasColumnName("email")
            .HasMaxLength(User.EmailMaxLength)
            .IsRequired();

        builder.Property(user => user.NormalizedEmail)
            .HasColumnName("normalized_email")
            .HasMaxLength(User.EmailMaxLength)
            .IsRequired();

        builder.HasIndex(user => user.NormalizedEmail)
            .HasDatabaseName("ux_users_normalized_email")
            .IsUnique();

        builder.Property(user => user.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(User.FullNameMaxLength)
            .IsRequired();

        builder.Property(user => user.Phone)
            .HasColumnName("phone")
            .HasMaxLength(User.PhoneMaxLength);

        // Глобальная роль вне организации: доступ к администрированию сервиса.
        builder.Property(user => user.IsServiceAdmin)
            .HasColumnName("is_service_admin")
            .IsRequired();

        builder.Property(user => user.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(user => user.SecurityStamp)
            .HasColumnName("security_stamp")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(user => user.ActiveOrganizationId)
            .HasColumnName("active_organization_id");

        builder.HasOne(user => user.ActiveOrganization)
            .WithMany()
            .HasForeignKey(user => user.ActiveOrganizationId)
            .HasConstraintName("fk_users_active_organization")
            .OnDelete(DeleteBehavior.SetNull);
    }
}
