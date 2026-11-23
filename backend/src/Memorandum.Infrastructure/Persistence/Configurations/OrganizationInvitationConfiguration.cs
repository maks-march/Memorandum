using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Memorandum.Domain.Organizations;

namespace Memorandum.Infrastructure.Persistence.Configurations;

public sealed class OrganizationInvitationConfiguration : IEntityTypeConfiguration<OrganizationInvitation>
{
    public void Configure(EntityTypeBuilder<OrganizationInvitation> builder)
    {
        builder.ToTable("organization_invitations");
        AuditColumns.Configure(builder);

        builder.Property(invitation => invitation.OrganizationId)
            .HasColumnName("organization_id")
            .IsRequired();

        builder.Property(invitation => invitation.Email)
            .HasColumnName("email")
            .HasMaxLength(OrganizationInvitation.EmailMaxLength)
            .IsRequired();

        builder.Property(invitation => invitation.NormalizedEmail)
            .HasColumnName("normalized_email")
            .HasMaxLength(OrganizationInvitation.EmailMaxLength)
            .IsRequired();

        builder.Property(invitation => invitation.Role)
            .HasColumnName("role")
            .HasMaxLength(16)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(invitation => invitation.Status)
            .HasColumnName("status")
            .HasMaxLength(16)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(invitation => invitation.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(invitation => invitation.AcceptedUserId)
            .HasColumnName("accepted_user_id");

        builder.Property(invitation => invitation.AcceptedAt)
            .HasColumnName("accepted_at");

        builder.HasOne(invitation => invitation.Organization)
            .WithMany(organization => organization.Invitations)
            .HasForeignKey(invitation => invitation.OrganizationId)
            .HasConstraintName("fk_organization_invitations_organizations")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Domain.Users.User>()
            .WithMany()
            .HasForeignKey(invitation => invitation.AcceptedUserId)
            .HasConstraintName("fk_organization_invitations_users")
            .OnDelete(DeleteBehavior.SetNull);

        // Одно ожидающее приглашение на адрес в организации: повторная отправка обновляет его.
        builder.HasIndex(invitation => new { invitation.OrganizationId, invitation.NormalizedEmail })
            .HasDatabaseName("ux_organization_invitations_pending_email")
            .HasFilter("\"status\" = 'Pending'")
            .IsUnique();

        // Поиск приглашения при регистрации идёт по нормализованному email.
        builder.HasIndex(invitation => invitation.NormalizedEmail)
            .HasDatabaseName("ix_organization_invitations_normalized_email");
    }
}
