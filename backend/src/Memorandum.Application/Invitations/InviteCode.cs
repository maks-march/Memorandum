using System.Security.Cryptography;
using System.Text;

namespace Memorandum.Application.Invitations;

/// <summary>
/// Код приглашения в организацию: 8 символов без похожих друг на друга
/// (нет 0/O, 1/I/L) — его диктуют голосом и вводят вручную.
/// </summary>
public static class InviteCode
{
    /// <summary>Алфавит без визуально неоднозначных символов.</summary>
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";

    public const int Length = 8;

    public static string Generate()
    {
        Span<char> code = stackalloc char[Length];

        for (var index = 0; index < Length; index++)
            code[index] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];

        return new string(code);
    }

    /// <summary>Пользователь мог ввести код строчными и с пробелами — нормализуем до проверки.</summary>
    public static string Normalize(string? code) =>
        (code ?? string.Empty).Trim().Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();

    /// <summary>
    /// В хранилище попадает HMAC с серверным pepper: код короткий, и из дампа Redis
    /// без pepper его можно перебрать за минуты.
    /// </summary>
    public static string Hash(string pepper, string code)
    {
        var payload = Encoding.UTF8.GetBytes($"invite:{Normalize(code)}");
        return Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(pepper), payload));
    }

}
