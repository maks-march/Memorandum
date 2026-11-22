using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Memorandum.Domain.Organizations;

namespace Memorandum.Infrastructure.Persistence.Configurations;

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations");
        AuditColumns.Configure(builder);

        builder.Property(organization => organization.Name)
            .HasColumnName("name")
            .HasMaxLength(Organization.NameMaxLength)
            .IsRequired();

        builder.Property(organization => organization.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        // Коллекция только для чтения: EF пишет в backing-поле, а не в свойство.
        builder.HasMany(organization => organization.Memberships)
            .WithOne(membership => membership.Organization)
            .HasForeignKey(membership => membership.OrganizationId)
            .HasConstraintName("fk_organization_memberships_organizations")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(organization => organization.Memberships)
            .HasField("_memberships")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
