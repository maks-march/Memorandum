using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Memorandum.Api.Domain.Organizations;
using Memorandum.Api.Domain.Users;

namespace Memorandum.Api.Infrastructure.Configurations;

public sealed class OrganizationMembershipConfiguration : IEntityTypeConfiguration<OrganizationMembership>
{
    public void Configure(EntityTypeBuilder<OrganizationMembership> builder)
    {
        builder.ToTable("organization_memberships");
        builder.UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");

        builder.Property(m => m.OrganizationId).HasColumnName("organization_id");
        builder.Property(m => m.UserId).HasColumnName("user_id");

        builder.Property(m => m.Role)
            .HasColumnName("role")
            .HasMaxLength(16)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(m => m.IsActive).HasColumnName("is_active");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.UserId)
            .HasDatabaseName("ux_organization_memberships_user_id_active")
            .HasFilter("\"is_active\"")
            .IsUnique();
    }
}
