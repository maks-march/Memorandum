using Memorandum.Domain.Users;
using AppVerification = Memorandum.Application.Abstractions.PasswordVerificationResult;
using IdentityVerification = Microsoft.AspNetCore.Identity.PasswordVerificationResult;

namespace Memorandum.Infrastructure.Security;

/// <summary>
/// Адаптер над <see cref="Microsoft.AspNetCore.Identity.PasswordHasher{TUser}"/>: полный ASP.NET Core
/// Identity не подключаем, нужен только алгоритм хеширования.
/// </summary>
public sealed class AspNetCorePasswordHasher : Memorandum.Application.Abstractions.IPasswordHasher
{
    private static readonly User HashUser = User.Create("hasher@example.ru", "Hasher", DateTime.UnixEpoch);

    private readonly Microsoft.AspNetCore.Identity.PasswordHasher<User> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(HashUser, password);

    public AppVerification Verify(string passwordHash, string password)
    {
        try
        {
            return _hasher.VerifyHashedPassword(HashUser, passwordHash, password) switch
            {
                IdentityVerification.Success => AppVerification.Success,
                IdentityVerification.SuccessRehashNeeded => AppVerification.SuccessRehashNeeded,
                _ => AppVerification.Failed,
            };
        }
        catch (FormatException)
        {
            // Испорченный или чужой хеш в БД — это неудачный вход, а не 500.
            return AppVerification.Failed;
        }
    }
}
