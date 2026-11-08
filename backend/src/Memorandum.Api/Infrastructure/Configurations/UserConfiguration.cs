using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Memorandum.Api.Domain.Organizations;
using Memorandum.Api.Domain.Users;

namespace Memorandum.Api.Infrastructure.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasMaxLength(254)
            .IsRequired();

        builder.Property(u => u.NormalizedEmail)
            .HasColumnName("normalized_email")
            .HasMaxLength(254)
            .IsRequired();
        builder.HasIndex(u => u.NormalizedEmail).IsUnique();

        builder.Property(u => u.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(u => u.Position)
            .HasColumnName("position")
            .HasMaxLength(200);

        builder.Property(u => u.IsActive).HasColumnName("is_active");
        builder.Property(u => u.IsServiceAdmin).HasColumnName("is_service_admin");

        builder.Property(u => u.SecurityStamp)
            .HasColumnName("security_stamp")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(u => u.ActiveOrganizationId).HasColumnName("active_organization_id");

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(u => u.ActiveOrganizationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
