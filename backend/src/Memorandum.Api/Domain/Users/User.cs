using System.Security.Cryptography;

namespace Memorandum.Api.Domain.Users;

/// <summary>Пользователь системы. Создаётся только через <see cref="Create"/>.</summary>
public sealed class User
{
    private User()
    {
    }

    /// <summary>Идентификатор, генерируется EF.</summary>
    public long Id { get; private set; }

    public string Email { get; private set; } = null!;

    /// <summary>Нормализованный email для сравнения и уникального индекса.</summary>
    public string NormalizedEmail { get; private set; } = null!;

    public string FullName { get; private set; } = null!;

    public string? Position { get; private set; }

    public bool IsActive { get; private set; }

    /// <summary>Меняется при блокировке и смене пароля: отзывает выданные токены/сессии.</summary>
    public string SecurityStamp { get; private set; } = null!;

    public long? ActiveOrganizationId { get; private set; }

    /// <summary>Сервисный администратор. Выставляется только внутри приложения, не из фабрики.</summary>
    public bool IsServiceAdmin { get; private set; }

    public static Fin<User> Create(string email, string fullName, string? position = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            return ApiErrors.EmailRequired;

        var normalizedEmail = EmailNormalizer.Normalize(email);
        if (!normalizedEmail.Contains('@'))
            return ApiErrors.InvalidEmail;

        if (string.IsNullOrWhiteSpace(fullName))
            return ApiErrors.FullNameRequired;

        return new User
        {
            Email = email.Trim(),
            NormalizedEmail = normalizedEmail,
            FullName = fullName.Trim(),
            Position = string.IsNullOrWhiteSpace(position) ? null : position.Trim(),
            IsActive = true,
            SecurityStamp = NewSecurityStamp()
        };
    }

    public void SetActiveOrganization(long? organizationId) => ActiveOrganizationId = organizationId;

    /// <summary>Правка профиля не должна разлогинивать: стамп не меняется.</summary>
    public Fin<Unit> ChangeProfile(string fullName, string? position)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return ApiErrors.FullNameRequired;

        FullName = fullName.Trim();
        Position = string.IsNullOrWhiteSpace(position) ? null : position.Trim();
        return Unit.Default;
    }

    public void Deactivate()
    {
        IsActive = false;
        RotateSecurityStamp();
    }

    public void Activate() => IsActive = true;

    /// <summary>Вызывается после смены пароля и при изменении ролей.</summary>
    public void RotateSecurityStamp() => SecurityStamp = NewSecurityStamp();

    private static string NewSecurityStamp() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
}
