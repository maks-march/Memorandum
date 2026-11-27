using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Memorandum.Domain.Documents;

namespace Memorandum.Infrastructure.Persistence.Configurations;

public sealed class DocumentParticipantConfiguration : IEntityTypeConfiguration<DocumentParticipant>
{
    public void Configure(EntityTypeBuilder<DocumentParticipant> builder)
    {
        builder.ToTable("document_participants");
        AuditColumns.Configure(builder);

        builder.Property(participant => participant.DocumentId)
            .HasColumnName("document_id")
            .IsRequired();

        builder.Property(participant => participant.Kind)
            .HasColumnName("kind")
            .HasMaxLength(16)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(participant => participant.PartyType)
            .HasColumnName("party_type")
            .HasMaxLength(16)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(participant => participant.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(DocumentParticipant.FullNameMaxLength)
            .IsRequired();

        builder.Property(participant => participant.Position)
            .HasColumnName("position")
            .HasMaxLength(DocumentParticipant.PositionMaxLength);

        builder.Property(participant => participant.Snils)
            .HasColumnName("snils")
            .HasMaxLength(DocumentParticipant.SnilsMaxLength);

        builder.Property(participant => participant.NrsNopriz)
            .HasColumnName("nrs_nopriz")
            .HasMaxLength(DocumentParticipant.NrsMaxLength);

        builder.Property(participant => participant.Order)
            .HasColumnName("sort_order")
            .IsRequired();

        builder.Property(participant => participant.OrganizationId)
            .HasColumnName("organization_id");

        builder.Property(participant => participant.UserId)
            .HasColumnName("user_id");

        // Ссылки на организации и пользователей необязательны: участник может быть внешним.
        builder.HasOne<Domain.Organizations.Organization>()
            .WithMany()
            .HasForeignKey(participant => participant.OrganizationId)
            .HasConstraintName("fk_document_participants_organizations")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<Domain.Users.User>()
            .WithMany()
            .HasForeignKey(participant => participant.UserId)
            .HasConstraintName("fk_document_participants_users")
            .OnDelete(DeleteBehavior.SetNull);

        // ГИП один: уникальный индекс не даёт завести второго в обход домена.
        builder.HasIndex(participant => new { participant.DocumentId, participant.Kind })
            .HasDatabaseName("ux_document_participants_document_id_kind")
            .IsUnique();

        builder.HasIndex(participant => participant.DocumentId)
            .HasDatabaseName("ix_document_participants_document_id");
    }
}
