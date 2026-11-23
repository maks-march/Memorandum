using System.Security.Cryptography;

/// <summary>
/// Генератор для данных
/// </summary>
public static class Generator
{
    /// <summary>
    /// Строковый код в base64
    /// </summary>
    /// <param name="byteLength">Байтовая длина кода.</param>
    /// <returns>Код ввиде строки.</returns>
    public static string GenerateCode(int byteLength)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteLength);
        // Base64 → URL-safe: '+' → '-', '/' → '_', убираем '='
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}