using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Memorandum.Domain.Schemas;

namespace Memorandum.Infrastructure.Persistence.Configurations;

public sealed class SchemaPackageFileConfiguration : IEntityTypeConfiguration<SchemaPackageFile>
{
    public void Configure(EntityTypeBuilder<SchemaPackageFile> builder)
    {
        builder.ToTable("schema_package_files");
        AuditColumns.Configure(builder);

        builder.Property(file => file.SchemaVersionId)
            .HasColumnName("schema_version_id")
            .IsRequired();

        builder.Property(file => file.Kind)
            .HasColumnName("kind")
            .HasMaxLength(16)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(file => file.Path)
            .HasColumnName("path")
            .HasMaxLength(SchemaPackageFile.PathMaxLength)
            .IsRequired();

        builder.Property(file => file.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(SchemaPackageFile.ContentTypeMaxLength)
            .IsRequired();

        // Содержимое комплекта: байты, а не ссылка на хранилище.
        builder.Property(file => file.Content)
            .HasColumnName("content")
            .HasColumnType("bytea")
            .IsRequired();

        builder.Property(file => file.Sha256)
            .HasColumnName("sha256")
            .HasMaxLength(SchemaVersion.Sha256Length)
            .IsRequired();

        builder.Ignore(file => file.SizeBytes);

        builder.HasOne(file => file.SchemaVersion)
            .WithMany(version => version.PackageFiles)
            .HasForeignKey(file => file.SchemaVersionId)
            .HasConstraintName("fk_schema_package_files_schema_versions")
            .OnDelete(DeleteBehavior.Cascade);

        // Путь уникален внутри версии: XSD ссылаются друг на друга по пути.
        builder.HasIndex(file => new { file.SchemaVersionId, file.Path })
            .HasDatabaseName("ux_schema_package_files_schema_version_id_path")
            .IsUnique();
    }
}
