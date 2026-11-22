using Memorandum.Domain.Common;

namespace Memorandum.Domain.Users;

/// <summary>
/// Хеш пароля, отдельный от <see cref="User"/>: профиль и учётные данные меняются разными сценариями,
/// а выборка профиля не должна случайно захватывать секрет.
/// </summary>
public sealed class Credential : AuditableEntity
{
    public const int PasswordHashMaxLength = 512;

    private Credential()
    {
    }

    public long UserId { get; private set; }

    public User User { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public static Credential Create(User user, string? passwordHash, DateTime utcNow, string? createdBy = null)
    {
        ArgumentNullException.ThrowIfNull(user);

        var credential = new Credential
        {
            UserId = user.Id,
            User = user,
            PasswordHash = Guard.Required(passwordHash, "Хеш пароля", PasswordHashMaxLength),
        };

        credential.MarkCreated(utcNow, createdBy);
        return credential;
    }

    /// <summary>Замена хеша. Стамп пользователя меняет вызывающий сценарий: домен не знает про сессии.</summary>
    public void ChangeHash(string? passwordHash, DateTime utcNow, string? updatedBy = null)
    {
        PasswordHash = Guard.Required(passwordHash, "Хеш пароля", PasswordHashMaxLength);
        Touch(utcNow, updatedBy);
    }
}
