using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Memorandum.Domain.Common;

namespace Memorandum.Infrastructure.Persistence;

/// <summary>Одинаковый набор колонок аудита для всех сущностей.</summary>
internal static class AuditColumns
{
    public static void Configure<T>(EntityTypeBuilder<T> builder)
        where T : AuditableEntity
    {
        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(entity => entity.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(entity => entity.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(entity => entity.UpdatedBy)
            .HasColumnName("updated_by")
            .HasMaxLength(AuditableEntity.UpdatedByMaxLength);
    }
}
