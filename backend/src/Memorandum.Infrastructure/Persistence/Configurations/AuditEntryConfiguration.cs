using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Memorandum.Domain.Admin;

namespace Memorandum.Infrastructure.Persistence.Configurations;

public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("audit_entries");
        AuditColumns.Configure(builder);

        builder.Property(entry => entry.Entity)
            .HasColumnName("entity")
            .HasMaxLength(AuditEntry.EntityMaxLength)
            .IsRequired();

        builder.Property(entry => entry.EntityId)
            .HasColumnName("entity_id");

        builder.Property(entry => entry.Action)
            .HasColumnName("action")
            .HasMaxLength(AuditEntry.ActionMaxLength)
            .IsRequired();

        builder.Property(entry => entry.UserId)
            .HasColumnName("user_id");

        builder.Property(entry => entry.OrganizationId)
            .HasColumnName("organization_id");

        // Значения полей до и после: структура зависит от сущности.
        builder.Property(entry => entry.PayloadJson)
            .HasColumnName("payload_json")
            .HasColumnType("jsonb")
            .IsRequired();

        // История изменений сущности и последние действия организации.
        builder.HasIndex(entry => new { entry.Entity, entry.EntityId, entry.CreatedAt })
            .HasDatabaseName("ix_audit_entries_entity_entity_id_created_at");

        builder.HasIndex(entry => new { entry.OrganizationId, entry.CreatedAt })
            .HasDatabaseName("ix_audit_entries_organization_id_created_at");
    }
}
