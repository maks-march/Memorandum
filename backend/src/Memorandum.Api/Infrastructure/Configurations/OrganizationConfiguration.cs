using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Memorandum.Api.Domain.Organizations;

namespace Memorandum.Api.Infrastructure.Configurations;

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations");
        builder.UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("id");

        builder.Property(o => o.Name)
            .HasColumnName("name")
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(o => o.IsActive).HasColumnName("is_active");

        builder.HasMany(o => o.Memberships)
            .WithOne()
            .HasForeignKey(m => m.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.Memberships)
            .HasField("_memberships")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
