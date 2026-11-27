using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Memorandum.Domain.Admin;

namespace Memorandum.Infrastructure.Persistence.Configurations;

public sealed class AppErrorConfiguration : IEntityTypeConfiguration<AppError>
{
    public void Configure(EntityTypeBuilder<AppError> builder)
    {
        builder.ToTable("app_errors");
        AuditColumns.Configure(builder);

        builder.Property(error => error.Level)
            .HasColumnName("level")
            .HasMaxLength(16)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(error => error.Code)
            .HasColumnName("code")
            .HasMaxLength(AppError.CodeMaxLength)
            .IsRequired();

        builder.Property(error => error.Message)
            .HasColumnName("message")
            .HasMaxLength(AppError.MessageMaxLength)
            .IsRequired();

        builder.Property(error => error.StackTrace)
            .HasColumnName("stack_trace")
            .HasMaxLength(AppError.StackTraceMaxLength);

        builder.Property(error => error.TraceId)
            .HasColumnName("trace_id")
            .HasMaxLength(AppError.TraceIdMaxLength);

        builder.Property(error => error.Path)
            .HasColumnName("path")
            .HasMaxLength(AppError.PathMaxLength);

        builder.Property(error => error.UserId)
            .HasColumnName("user_id");

        builder.Property(error => error.OrganizationId)
            .HasColumnName("organization_id");

        builder.Property(error => error.RepeatCount)
            .HasColumnName("repeat_count")
            .IsRequired();

        builder.Property(error => error.LastOccurredAt)
            .HasColumnName("last_occurred_at");

        // Экран /admin/errors: свежие ошибки и фильтр по коду.
        builder.HasIndex(error => new { error.Code, error.LastOccurredAt })
            .HasDatabaseName("ix_app_errors_code_last_occurred_at");
    }
}
