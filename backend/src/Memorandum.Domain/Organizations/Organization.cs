using Memorandum.Domain.Common;
using Memorandum.Domain.Users;

namespace Memorandum.Domain.Organizations;

/// <summary>Проектная организация или другая организация, использующая сервис.</summary>
public sealed class Organization : AuditableEntity
{
    public const int NameMaxLength = 200;
    public const int InnMaxLength = 12;
    public const int KppMaxLength = 9;
    public const int OgrnMaxLength = 15;
    public const int AddressMaxLength = 500;

    private readonly List<OrganizationMembership> _memberships = [];
    private readonly List<OrganizationInvitation> _invitations = [];

    private Organization()
    {
    }

    public string Name { get; private set; } = null!;

    /// <summary>ИНН: 10 цифр у юридического лица, 12 у индивидуального предпринимателя.</summary>
    public string? Inn { get; private set; }

    /// <summary>КПП: 9 цифр у юридического лица; у ИП отсутствует.</summary>
    public string? Kpp { get; private set; }

    /// <summary>ОГРН: 13 цифр у юридического лица, 15 у ИП.</summary>
    public string? Ogrn { get; private set; }

    public string? Address { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<OrganizationMembership> Memberships => _memberships;

    /// <summary>Приглашения организации: ожидающие, принятые и отозванные.</summary>
    public IReadOnlyCollection<OrganizationInvitation> Invitations => _invitations;

    public static Organization Create(string? name, DateTime utcNow, string? createdBy = null)
    {
        var organization = new Organization
        {
            Name = Guard.Required(name, "Название организации", NameMaxLength),
            IsActive = true,
        };

        organization.MarkCreated(utcNow, createdBy);
        return organization;
    }

    public void Rename(string? name, DateTime utcNow, string? updatedBy = null)
    {
        Name = Guard.Required(name, "Название организации", NameMaxLength);
        Touch(utcNow, updatedBy);
    }

    /// <summary>
    /// Реквизиты. Каждое поле проверяется отдельно: ИНН/КПП/ОГРН — только цифры
    /// установленной длины, иначе документ не пройдёт проверку в экспертизе.
    /// </summary>
    public void ChangeDetails(
        string? inn,
        string? kpp,
        string? ogrn,
        string? address,
        DateTime utcNow,
        string? updatedBy = null)
    {
        Inn = Guard.Optional(Digits(inn, "ИНН", InnMaxLength, [10, 12]), "ИНН", InnMaxLength);
        Kpp = Guard.Optional(Digits(kpp, "КПП", KppMaxLength, [9]), "КПП", KppMaxLength);
        Ogrn = Guard.Optional(Digits(ogrn, "ОГРН", OgrnMaxLength, [13, 15]), "ОГРН", OgrnMaxLength);
        Address = Guard.Optional(address, "Адрес", AddressMaxLength);
        Touch(utcNow, updatedBy);
    }

    /// <summary>
    /// Добавляет участника.
    /// Ранее исключённый пользователь возвращается с новой ролью — второй записи на того же пользователя не появляется.
    /// </summary>
    public OrganizationMembership AddMember(
        User user,
        OrganizationRole role,
        DateTime utcNow,
        string? createdBy = null,
        string? position = null)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (!IsActive)
            throw new ConflictException(ErrorCodes.OrganizationInactive, "Организация деактивирована.");

        var existing = _memberships.FirstOrDefault(m => m.UserId == user.Id && user.Id > 0)
                       ?? _memberships.FirstOrDefault(m => ReferenceEquals(m.User, user));

        if (existing is { IsActive: true })
            throw new ConflictException(ErrorCodes.MemberAlreadyExists, "Пользователь уже состоит в организации.");

        if (existing is not null)
        {
            existing.Reactivate(role, utcNow, createdBy);
            existing.ChangePosition(position, utcNow, createdBy);
            return existing;
        }

        var membership = new OrganizationMembership(user, role, utcNow, createdBy);
        membership.ChangePosition(position, utcNow, createdBy);
        _memberships.Add(membership);
        return membership;
    }

    public void ChangeRole(long userId, OrganizationRole role, DateTime utcNow, string? updatedBy = null)
    {
        var membership = FindActive(userId);

        if (membership.Role == role)
            return;

        EnsureNotLastAdmin(membership, "Нельзя понизить последнего администратора организации.");

        membership.ChangeRole(role, utcNow, updatedBy);
    }

    /// <summary>Должность сотрудника видна только в контексте организации, поэтому меняется через неё.</summary>
    public void ChangePosition(long userId, string? position, DateTime utcNow, string? updatedBy = null) =>
        FindActive(userId).ChangePosition(position, utcNow, updatedBy);

    /// <summary>Исключает пользователя; последнего администратора исключить нельзя.</summary>
    public void RemoveMember(long userId, DateTime utcNow, string? updatedBy = null)
    {
        var membership = FindActive(userId);
        EnsureNotLastAdmin(membership, "Нельзя исключить последнего администратора организации.");
        membership.Deactivate(utcNow, updatedBy);
    }

    /// <summary>Организация больше не принимает участников.</summary>
    public void Deactivate(DateTime utcNow, string? updatedBy = null)
    {
        IsActive = false;
        Touch(utcNow, updatedBy);
    }

    /// <summary>
    /// Роль, которую можно выдать по коду приглашения. Администратором делает
    /// только действующий администратор организации, а не всякий, кто знает код.
    /// </summary>
    public static bool CanInviteWithRole(OrganizationRole role) =>
        role is OrganizationRole.Editor or OrganizationRole.Viewer;

    /// <summary>
    /// Приглашение сотрудника по email. Одно ожидающее приглашение на адрес:
    /// повторная отправка обновляет роль и срок, а не плодит записи.
    /// </summary>
    public OrganizationInvitation Invite(
        string? email,
        OrganizationRole role,
        DateTime expiresAt,
        DateTime utcNow,
        string? createdBy = null)
    {
        if (!IsActive)
            throw new ConflictException(ErrorCodes.OrganizationInactive, "Организация деактивирована.");

        if (!CanInviteWithRole(role))
            throw new ValidationException(
                ErrorCodes.Validation,
                "По приглашению можно выдать роль Editor или Viewer.");

        var normalized = EmailNormalizer.Normalize(Guard.Required(email, "Email", OrganizationInvitation.EmailMaxLength));

        if (_invitations.FirstOrDefault(invitation =>
                invitation.NormalizedEmail == normalized && invitation.IsPending) is { } pending)
        {
            pending.Extend(expiresAt, utcNow, createdBy);
            return pending;
        }

        var created = new OrganizationInvitation(email!, role, expiresAt, utcNow, createdBy);
        _invitations.Add(created);
        return created;
    }

    /// <summary>Приглашение принято: пользователь становится участником с ролью из приглашения.</summary>
    public OrganizationMembership AcceptInvitation(
        string? email,
        User user,
        DateTime utcNow,
        string? updatedBy = null)
    {
        ArgumentNullException.ThrowIfNull(user);

        var invitation = FindPendingInvitation(email);
        invitation.Accept(user, utcNow, updatedBy);

        return AddMember(user, invitation.Role, utcNow, updatedBy);
    }

    public void RevokeInvitation(string? email, DateTime utcNow, string? updatedBy = null) =>
        FindPendingInvitation(email).Revoke(utcNow, updatedBy);

    /// <summary>Есть ли действующее приглашение на адрес.</summary>
    public bool HasPendingInvitation(string? email)
    {
        var normalized = EmailNormalizer.Normalize(email);

        return _invitations.Any(invitation =>
            invitation.IsPending && invitation.NormalizedEmail == normalized);
    }

    private OrganizationInvitation FindPendingInvitation(string? email)
    {
        var normalized = EmailNormalizer.Normalize(Guard.Required(email, "Email", OrganizationInvitation.EmailMaxLength));

        return _invitations.FirstOrDefault(invitation =>
                   invitation.IsPending && invitation.NormalizedEmail == normalized)
               ?? throw new NotFoundException(ErrorCodes.NotFound, "Приглашение не найдено.");
    }

    /// <summary>Есть ли у пользователя активное участие — по нему строится изоляция данных.</summary>
    public bool HasActiveMember(long userId) =>
        userId > 0 && _memberships.Any(membership => membership.IsActive && membership.UserId == userId);

    private OrganizationMembership FindActive(long userId)
    {
        Guard.PositiveId(userId, "Пользователь");

        var membership = _memberships.FirstOrDefault(m => m.UserId == userId && m.IsActive)
                         ?? throw new NotFoundException(ErrorCodes.NotFound, "Пользователь не состоит в организации.");

        return membership;
    }

    private void EnsureNotLastAdmin(OrganizationMembership membership, string message)
    {
        var admins = _memberships.Count(m => m.IsActive && m.Role == OrganizationRole.OrgAdmin);

        if (membership.Role == OrganizationRole.OrgAdmin && admins == 1)
            throw new ConflictException(ErrorCodes.LastOrgAdminRequired, message);
    }

    /// <summary>Реквизит: только цифры и одна из допустимых длин. Пустое значение — <c>null</c>.</summary>
    private static string? Digits(string? value, string fieldName, int maxLength, int[] lengths)
    {
        var trimmed = Guard.Optional(value, fieldName, maxLength);

        if (trimmed is null)
            return null;

        if (!trimmed.All(char.IsAsciiDigit) || !lengths.Contains(trimmed.Length))
            throw new ValidationException(
                ErrorCodes.Validation,
                $"Поле «{fieldName}» должно состоять из {string.Join(" или ", lengths)} цифр.");

        return trimmed;
    }
}
