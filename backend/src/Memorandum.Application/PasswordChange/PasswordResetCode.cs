using System.Security.Cryptography;
using System.Text;

namespace Memorandum.Application.PasswordChange;

/// <summary>Код подтверждения: 6 цифр; в хранилище попадает только HMAC с серверным pepper.</summary>
public static class PasswordResetCode
{
    public const int Length = 6;

    public static string Generate() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString($"D{Length}");

    /// <summary>6 цифр — миллион вариантов: без pepper простой SHA-256 подбирается из дампа хранилища.</summary>
    public static string Hash(string pepper, long userId, string code)
    {
        var payload = Encoding.UTF8.GetBytes($"{userId}:{code}");
        return Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(pepper), payload));
    }

    /// <summary>Сравнение постоянного времени: утечка по таймингу теоретическая, но защита бесплатная.</summary>
    public static bool FixedTimeEquals(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(left), Encoding.UTF8.GetBytes(right));
}
