using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Memorandum.Domain.Users;

namespace Memorandum.Infrastructure.Persistence.Configurations;

public sealed class CredentialConfiguration : IEntityTypeConfiguration<Credential>
{
    public void Configure(EntityTypeBuilder<Credential> builder)
    {
        builder.ToTable("credentials");
        AuditColumns.Configure(builder);

        builder.Property(credential => credential.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(credential => credential.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(Credential.PasswordHashMaxLength)
            .IsRequired();

        builder.HasOne(credential => credential.User)
            .WithOne()
            .HasForeignKey<Credential>(credential => credential.UserId)
            .HasConstraintName("fk_credentials_users")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(credential => credential.UserId)
            .HasDatabaseName("ux_credentials_user_id")
            .IsUnique();
    }
}
