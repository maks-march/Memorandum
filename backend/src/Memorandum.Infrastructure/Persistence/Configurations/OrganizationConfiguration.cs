using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
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

        builder.Property(organization => organization.Inn)
            .HasColumnName("inn")
            .HasMaxLength(Organization.InnMaxLength);

        builder.Property(organization => organization.Kpp)
            .HasColumnName("kpp")
            .HasMaxLength(Organization.KppMaxLength);

        builder.Property(organization => organization.Ogrn)
            .HasColumnName("ogrn")
            .HasMaxLength(Organization.OgrnMaxLength);

        builder.Property(organization => organization.Address)
            .HasColumnName("address")
            .HasMaxLength(Organization.AddressMaxLength);

        builder.Property(organization => organization.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        // Иностранная организация: другая модель адреса в XML.
        builder.Property(organization => organization.IsForeign)
            .HasColumnName("is_foreign")
            .IsRequired();

        // НРС НОПРИЗ заменил членство в СРО начиная с версии схемы 01.07.
        builder.Property(organization => organization.NrsNopriz)
            .HasColumnName("nrs_nopriz")
            .HasMaxLength(Organization.NrsNoprizMaxLength);

        builder.Property(organization => organization.SelfRegulatoryOrganization)
            .HasColumnName("self_regulatory_organization")
            .HasMaxLength(Organization.AddressMaxLength);

        // Коллекция только для чтения: EF пишет в backing-поле, а не в свойство.
        builder.HasMany(organization => organization.Memberships)
            .WithOne(membership => membership.Organization)
            .HasForeignKey(membership => membership.OrganizationId)
            .HasConstraintName("fk_organization_memberships_organizations")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(organization => organization.Memberships)
            .HasField("_memberships")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(organization => organization.Invitations)
            .WithOne(invitation => invitation.Organization)
            .HasForeignKey(invitation => invitation.OrganizationId)
            .HasConstraintName("fk_organization_invitations_organizations")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(organization => organization.Invitations)
            .HasField("_invitations")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
