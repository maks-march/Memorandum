using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Memorandum.Domain.Dictionaries;

namespace Memorandum.Infrastructure.Persistence.Configurations;

public sealed class DictionaryConfiguration : IEntityTypeConfiguration<Dictionary>
{
    public void Configure(EntityTypeBuilder<Dictionary> builder)
    {
        builder.ToTable("dictionaries");
        AuditColumns.Configure(builder);

        builder.Property(item => item.Kind)
            .HasColumnName("kind")
            .HasMaxLength(16)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(item => item.Code)
            .HasColumnName("code")
            .HasMaxLength(Dictionary.CodeMaxLength)
            .IsRequired();

        builder.Property(item => item.Name)
            .HasColumnName("name")
            .HasMaxLength(Dictionary.NameMaxLength)
            .IsRequired();

        builder.Property(item => item.ParentCode)
            .HasColumnName("parent_code")
            .HasMaxLength(Dictionary.ParentCodeMaxLength);

        builder.Property(item => item.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(item => item.Version)
            .HasColumnName("version")
            .HasMaxLength(Dictionary.VersionMaxLength)
            .IsRequired();

        builder.Property(item => item.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired();

        // Код уникален внутри вида справочника: поиск идёт по паре.
        builder.HasIndex(item => new { item.Kind, item.Code })
            .HasDatabaseName("ux_dictionaries_kind_code")
            .IsUnique();

        // Выборка элементов справочника: вид, активность, порядок.
        builder.HasIndex(item => new { item.Kind, item.IsActive, item.SortOrder })
            .HasDatabaseName("ix_dictionaries_kind_is_active_sort_order");

        // Подсказки ОКТМО фильтруются по региону.
        builder.HasIndex(item => new { item.Kind, item.ParentCode })
            .HasDatabaseName("ix_dictionaries_kind_parent_code");
    }
}
