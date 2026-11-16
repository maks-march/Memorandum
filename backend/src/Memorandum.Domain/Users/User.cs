using Memorandum.Domain.Common;

namespace Memorandum.Domain.Users;

/// <summary>Пользователь системы. Создаётся только через <see cref="Create"/>.</summary>
public sealed class User : AuditableEntity
{
    public const int EmailMaxLength = 254;
    public const int FullNameMaxLength = 200;

    private User()
    {
    }

    public string Email { get; private set; } = null!;

    /// <summary>Нормализованный email: по нему уникальный индекс и поиск при входе.</summary>
    public string NormalizedEmail { get; private set; } = null!;

    public string FullName { get; private set; } = null!;

    public bool IsActive { get; private set; }

    /// <summary>Меняется при блокировке и смене пароля: отзывает уже выданные cookie.</summary>
    public string SecurityStamp { get; private set; } = null!;

    /// <summary>Организация, выбранная пользователем как текущая.</summary>
    public long? ActiveOrganizationId { get; private set; }

    public static User Create(string? email, string? fullName, string? createdBy = null)
    {
        var user = new User
        {
            Email = Guard.Required(email, "Email", EmailMaxLength),
            FullName = Guard.Required(fullName, "ФИО", FullNameMaxLength),
            IsActive = true,
            SecurityStamp = NewStamp(),
        };

        user.NormalizedEmail = EmailNormalizer.Normalize(user.Email);
        user.MarkCreated(createdBy);
        return user;
    }

    public void Rename(string? fullName, string? updatedBy = null)
    {
        FullName = Guard.Required(fullName, "ФИО", FullNameMaxLength);
        Touch(updatedBy);
    }

    /// <summary>Блокировка: доступ по уже выданным cookie отзывается через смену стампа.</summary>
    public void Deactivate(string? updatedBy = null)
    {
        IsActive = false;
        RotateStamp();
        Touch(updatedBy);
    }

    public void Activate(string? updatedBy = null)
    {
        IsActive = true;
        Touch(updatedBy);
    }

    /// <summary>Смена пароля и ролей: все выданные cookie становятся недействительными.</summary>
    public void ChangeSecurityStamp(string? updatedBy = null)
    {
        RotateStamp();
        Touch(updatedBy);
    }

    public void SetActiveOrganization(long? organizationId, string? updatedBy = null)
    {
        if (organizationId is not null)
            Guard.PositiveId(organizationId.Value, "Организация");

        ActiveOrganizationId = organizationId;
        Touch(updatedBy);
    }

    private void RotateStamp() => SecurityStamp = NewStamp();

    private static string NewStamp() => Guid.NewGuid().ToString("N");
}
