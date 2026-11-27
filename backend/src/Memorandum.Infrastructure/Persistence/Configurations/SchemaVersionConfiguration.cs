using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Memorandum.Domain.Schemas;

namespace Memorandum.Infrastructure.Persistence.Configurations;

public sealed class SchemaVersionConfiguration : IEntityTypeConfiguration<SchemaVersion>
{
    public void Configure(EntityTypeBuilder<SchemaVersion> builder)
    {
        builder.ToTable("schema_versions");
        AuditColumns.Configure(builder);

        // Код версии — идентификатор в API («01.07»), поэтому он и уникальный ключ.
        builder.Property(version => version.Code)
            .HasColumnName("code")
            .HasMaxLength(SchemaVersion.CodeMaxLength)
            .IsRequired();

        builder.HasIndex(version => version.Code)
            .HasDatabaseName("ux_schema_versions_code")
            .IsUnique();

        builder.Property(version => version.Title)
            .HasColumnName("title")
            .HasMaxLength(SchemaVersion.TitleMaxLength)
            .IsRequired();

        builder.Property(version => version.EffectiveFrom)
            .HasColumnName("effective_from")
            .IsRequired();

        builder.Property(version => version.PublishedAt)
            .HasColumnName("published_at");

        builder.Property(version => version.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(version => version.IsDefault)
            .HasColumnName("is_default")
            .IsRequired();

        builder.Property(version => version.Sha256)
            .HasColumnName("sha256")
            .HasMaxLength(SchemaVersion.Sha256Length);

        builder.Property(version => version.EntryPoint)
            .HasColumnName("entry_point")
            .HasMaxLength(SchemaVersion.EntryPointMaxLength);

        builder.Property(version => version.TargetNamespace)
            .HasColumnName("target_namespace")
            .HasMaxLength(SchemaVersion.NamespaceMaxLength);

        builder.Property(version => version.CompileMessage)
            .HasColumnName("compile_message")
            .HasMaxLength(SchemaVersion.CompileMessageMaxLength);

        builder.Property(version => version.CompiledAt)
            .HasColumnName("compiled_at");

        builder.Ignore(version => version.IsRunnable);

        // Версия по умолчанию одна: частичный уникальный индекс не даёт завести вторую.
        builder.HasIndex(version => version.IsDefault)
            .HasDatabaseName("ux_schema_versions_is_default")
            .IsUnique()
            .HasFilter("is_default");

        // Файлы комплекта — значение-объекты версии: своя таблица, но без отдельного ключа.
        builder.OwnsMany(version => version.Files, files =>
        {
            files.ToTable("schema_files");
            files.WithOwner().HasForeignKey("schema_version_id");
            files.Property(file => file.Kind)
                .HasColumnName("kind")
                .HasMaxLength(16)
                .HasConversion<string>()
                .IsRequired();
            files.Property(file => file.Path)
                .HasColumnName("path")
                .HasMaxLength(SchemaFile.PathMaxLength)
                .IsRequired();
            files.Property(file => file.Name)
                .HasColumnName("name")
                .HasMaxLength(SchemaFile.NameMaxLength)
                .IsRequired();
            files.Property(file => file.SizeBytes).HasColumnName("size_bytes").IsRequired();
            files.Property(file => file.Sha256)
                .HasColumnName("sha256")
                .HasMaxLength(SchemaVersion.Sha256Length)
                .IsRequired();
            files.Property(file => file.StoredKey)
                .HasColumnName("stored_key")
                .HasMaxLength(SchemaFile.PathMaxLength);
            files.HasKey("schema_version_id", nameof(SchemaFile.Path));
        });

        builder.OwnsMany(version => version.Fields, fields =>
        {
            fields.ToTable("schema_fields");
            fields.WithOwner().HasForeignKey("schema_version_id");
            fields.Property(field => field.FieldId)
                .HasColumnName("field_id")
                .HasMaxLength(SchemaField.FieldIdMaxLength)
                .IsRequired();
            fields.Property(field => field.Section)
                .HasColumnName("section")
                .HasMaxLength(SchemaField.SectionMaxLength)
                .IsRequired();
            fields.Property(field => field.Label)
                .HasColumnName("label")
                .HasMaxLength(SchemaField.LabelMaxLength)
                .IsRequired();
            fields.Property(field => field.Kind)
                .HasColumnName("kind")
                .HasMaxLength(16)
                .HasConversion<string>()
                .IsRequired();
            fields.Property(field => field.Required).HasColumnName("required").IsRequired();
            fields.Property(field => field.Dictionary)
                .HasColumnName("dictionary")
                .HasMaxLength(SchemaField.DictionaryMaxLength);
            fields.Property(field => field.Format)
                .HasColumnName("format")
                .HasMaxLength(SchemaField.FormatMaxLength);
            fields.Property(field => field.Hint)
                .HasColumnName("hint")
                .HasMaxLength(SchemaField.HintMaxLength);
            fields.Property(field => field.Group)
                .HasColumnName("field_group")
                .HasMaxLength(SchemaField.GroupMaxLength);
            fields.Property(field => field.XmlPath)
                .HasColumnName("xml_path")
                .HasMaxLength(SchemaField.XmlPathMaxLength);
            fields.Property(field => field.SortOrder).HasColumnName("sort_order").IsRequired();
            fields.HasKey("schema_version_id", nameof(SchemaField.FieldId));
        });
    }
}
