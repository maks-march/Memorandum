using Memorandum.Domain.Common;
using Memorandum.Domain.Organizations;

namespace Memorandum.Domain.Users;

/// <summary>Пользователь системы. Создаётся только через <see cref="Create"/>.</summary>
public sealed class User : AuditableEntity
{
    public const int EmailMaxLength = 254;
    public const int FullNameMaxLength = 200;
    public const int PhoneMaxLength = 20;

    private User()
    {
    }

    public string Email { get; private set; } = null!;

    /// <summary>Нормализованный email: по нему уникальный индекс и поиск при входе.</summary>
    public string NormalizedEmail { get; private set; } = null!;

    public string FullName { get; private set; } = null!;

    /// <summary>Телефон для связи; формат свободный, проверяется только длина.</summary>
    public string? Phone { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>
    /// Администратор сервиса: глобальная роль вне организации. Видит организации,
    /// пользователей и версии схем, но не содержимое документов чужих организаций.
    /// </summary>
    public bool IsServiceAdmin { get; private set; }

    /// <summary>Меняется при блокировке и смене пароля: отзывает уже выданные cookie.</summary>
    public string SecurityStamp { get; private set; } = null!;

    /// <summary>Организация, выбранная пользователем как текущую.</summary>
    public long? ActiveOrganizationId { get; private set; }

    /// <summary>
    /// Навигация для сохранения в один <c>SaveChangesAsync</c>: при регистрации организация
    /// ещё не имеет Id, и FK проставит EF после вставки.
    /// </summary>
    public Organization? ActiveOrganization { get; private set; }

    public static User Create(string? email, string? fullName, DateTime utcNow, string? createdBy = null)
    {
        var user = new User
        {
            Email = Guard.Required(email, "Email", EmailMaxLength),
            FullName = Guard.Required(fullName, "ФИО", FullNameMaxLength),
            IsActive = true,
            SecurityStamp = NewStamp(),
        };

        user.NormalizedEmail = EmailNormalizer.Normalize(user.Email);
        user.MarkCreated(utcNow, createdBy);
        return user;
    }

    /// <summary>Выдача и снятие роли администратора сервиса.</summary>
    public void SetServiceAdmin(bool isServiceAdmin, DateTime utcNow, string? updatedBy = null)
    {
        if (IsServiceAdmin == isServiceAdmin)
            return;

        IsServiceAdmin = isServiceAdmin;
        Touch(utcNow, updatedBy);
    }

    public void Rename(string? fullName, DateTime utcNow, string? updatedBy = null)
    {
        FullName = Guard.Required(fullName, "ФИО", FullNameMaxLength);
        Touch(utcNow, updatedBy);
    }

    /// <summary>Личные данные профиля. Email меняется отдельно: по нему вход и уникальный индекс.</summary>
    public void ChangeProfile(string? fullName, string? phone, DateTime utcNow, string? updatedBy = null)
    {
        FullName = Guard.Required(fullName, "ФИО", FullNameMaxLength);
        Phone = Guard.Optional(phone, "Телефон", PhoneMaxLength);
        Touch(utcNow, updatedBy);
    }

    /// <summary>Блокировка: доступ по уже выданным cookie отзывается через смену стампа.</summary>
    public void Deactivate(DateTime utcNow, string? updatedBy = null)
    {
        IsActive = false;
        RotateStamp();
        Touch(utcNow, updatedBy);
    }

    public void Activate(DateTime utcNow, string? updatedBy = null)
    {
        IsActive = true;
        Touch(utcNow, updatedBy);
    }

    /// <summary>Смена пароля и ролей: все выданные cookie становятся недействительными.</summary>
    public void ChangeSecurityStamp(DateTime utcNow, string? updatedBy = null)
    {
        RotateStamp();
        Touch(utcNow, updatedBy);
    }

    public void SetActiveOrganization(long? organizationId, DateTime utcNow, string? updatedBy = null)
    {
        if (organizationId is not null)
            Guard.PositiveId(organizationId.Value, "Организация");

        ActiveOrganizationId = organizationId;
        Touch(utcNow, updatedBy);
    }

    /// <summary>
    /// Выбор текущей организации по сущности — путь для сценария, где Id ещё не присвоен.
    /// Внешний ключ заполняет EF после вставки организации.
    /// </summary>
    public void SetActiveOrganization(Organization organization, DateTime utcNow, string? updatedBy = null)
    {
        ArgumentNullException.ThrowIfNull(organization);

        ActiveOrganization = organization;
        Touch(utcNow, updatedBy);
    }

    private void RotateStamp() => SecurityStamp = NewStamp();

    private static string NewStamp() => Guid.NewGuid().ToString("N");
}
