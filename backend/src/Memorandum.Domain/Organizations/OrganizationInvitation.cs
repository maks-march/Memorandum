using Memorandum.Domain.Common;
using Memorandum.Domain.Users;

namespace Memorandum.Domain.Organizations;

/// <summary>
/// Приглашение сотрудника по email. Часть агрегата <see cref="Organizations.Organization"/>:
/// создаётся и отзывается только через неё.
/// <para>
/// Отличается от кода приглашения: код выдаётся на организацию и вводится при регистрации,
/// а приглашение адресовано конкретному человеку и живёт до принятия или отзыва.
/// </para>
/// </summary>
public sealed class OrganizationInvitation : AuditableEntity
{
    public const int EmailMaxLength = User.EmailMaxLength;

    private OrganizationInvitation()
    {
    }

    internal OrganizationInvitation(string email, OrganizationRole role, DateTime expiresAt, DateTime utcNow, string? createdBy)
    {
        Email = Guard.Required(email, "Email", EmailMaxLength);
        NormalizedEmail = EmailNormalizer.Normalize(Email);

        // Срок — в будущем, иначе приглашение мертво с момента выдачи.
        if (expiresAt <= utcNow)
            throw new ValidationException(
                ErrorCodes.Validation,
                "Срок действия приглашения должен быть в будущем.");

        Role = role;
        ExpiresAt = expiresAt;
        Status = InvitationStatus.Pending;
        MarkCreated(utcNow, createdBy);
    }

    public long OrganizationId { get; private set; }

    public Organization Organization { get; private set; } = null!;

    /// <summary>Email приглашённого в том виде, в каком его ввели.</summary>
    public string Email { get; private set; } = null!;

    /// <summary>Нормализованный email: по нему приглашение находится при регистрации.</summary>
    public string NormalizedEmail { get; private set; } = null!;

    /// <summary>Роль, которая будет выдана при принятии: администратора по приглашению не дают.</summary>
    public OrganizationRole Role { get; private set; }

    public InvitationStatus Status { get; private set; }

    /// <summary>Когда приглашение перестаёт действовать; продлевается отдельным действием.</summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>Кто и когда принял приглашение; <c>null</c>, пока оно не принято.</summary>
    public long? AcceptedUserId { get; private set; }

    public DateTime? AcceptedAt { get; private set; }

    public bool IsPending => Status == InvitationStatus.Pending;

    internal void Accept(User user, DateTime utcNow, string? updatedBy)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (!IsPending)
            throw new ConflictException(ErrorCodes.InvitationAlreadyHandled, "Приглашение уже обработано.");

        if (ExpiresAt <= utcNow)
            throw new ConflictException(ErrorCodes.InvitationExpired, "Срок действия приглашения истёк.");

        Status = InvitationStatus.Accepted;
        AcceptedUserId = user.Id;
        AcceptedAt = utcNow;
        Touch(utcNow, updatedBy ?? user.Email);
    }

    internal void Revoke(DateTime utcNow, string? updatedBy)
    {
        if (!IsPending)
            throw new ConflictException(ErrorCodes.InvitationAlreadyHandled, "Приглашение уже обработано.");

        Status = InvitationStatus.Revoked;
        Touch(utcNow, updatedBy);
    }

    internal void Extend(DateTime expiresAt, DateTime utcNow, string? updatedBy)
    {
        if (!IsPending)
            throw new ConflictException(ErrorCodes.InvitationAlreadyHandled, "Приглашение уже обработано.");

        ExpiresAt = expiresAt;
        Touch(utcNow, updatedBy);
    }
}
