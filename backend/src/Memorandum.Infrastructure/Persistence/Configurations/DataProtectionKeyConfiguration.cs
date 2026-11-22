using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Memorandum.Infrastructure.Persistence.DataProtection;

namespace Memorandum.Infrastructure.Persistence.Configurations;

public sealed class DataProtectionKeyConfiguration : IEntityTypeConfiguration<DataProtectionKey>
{
    public void Configure(EntityTypeBuilder<DataProtectionKey> builder)
    {
        builder.ToTable("data_protection_keys");
        AuditColumns.Configure(builder);

        builder.Property(key => key.FriendlyName)
            .HasColumnName("friendly_name")
            .HasMaxLength(DataProtectionKey.FriendlyNameMaxLength)
            .IsRequired();

        builder.Property(key => key.Xml)
            .HasColumnName("xml")
            .HasColumnType("text")
            .IsRequired();

        builder.HasIndex(key => key.FriendlyName)
            .HasDatabaseName("ux_data_protection_keys_friendly_name")
            .IsUnique();
    }
}
