using Memorandum.Domain.Common;

namespace Memorandum.Domain.Users;

/// <summary>
/// Нормализация email для сравнения и уникального индекса: обрезка пробелов и нижний регистр.
/// Отдельный класс, а не value object: в сущности две колонки — <c>Email</c> и <c>NormalizedEmail</c>.
/// </summary>
public static class EmailNormalizer
{
    public static string Normalize(string? email) => Guard.Required(email, "Email").ToLowerInvariant();
}
