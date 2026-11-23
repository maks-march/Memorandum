namespace Memorandum.Domain.Common;

/// <summary>Проверки входных значений сущностей. Нарушение — <see cref="DomainException"/>.</summary>
public static class Guard
{
    /// <summary>Обязательная строка: обрезает пробелы, проверяет длину.</summary>
    public static string Required(string? value, string fieldName, int? maxLength = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationException(ErrorCodes.Required, $"Поле «{fieldName}» обязательно для заполнения.");

        var trimmed = value.Trim();
        if (maxLength.HasValue && trimmed.Length > maxLength.Value)
            throw new ValidationException(
                ErrorCodes.TooLong,
                $"Поле «{fieldName}» не должно быть длиннее {maxLength} символов.");

        return trimmed;
    }

    /// <summary>Необязательная строка: пустое значение превращается в <c>null</c>.</summary>
    public static string? Optional(string? value, string fieldName, int? maxLength = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (maxLength.HasValue && trimmed.Length > maxLength.Value)
            throw new ValidationException(
                ErrorCodes.TooLong,
                $"Поле «{fieldName}» не должно быть длиннее {maxLength} символов.");

        return trimmed;
    }

    /// <summary>Идентификатор должен быть уже присвоен (положительный).</summary>
    public static long PositiveId(long value, string fieldName)
    {
        if (value <= 0)
            throw new ValidationException(
                ErrorCodes.Validation,
                $"Поле «{fieldName}» должно содержать идентификатор больше нуля.");

        return value;
    }

    /// <summary>Дата не может быть в будущем: домен живёт по UTC, сравнение строгое.</summary>
    public static DateTime NotInFuture(DateTime value, string fieldName, DateTime utcNow)
    {
        if (value > utcNow)
            throw new ValidationException(
                ErrorCodes.Validation,
                $"Поле «{fieldName}» не может быть в будущем.");

        return value;
    }

    /// <summary>Дата окончания не раньше даты начала.</summary>
    public static void DateRange(DateTime from, DateTime? to, string fieldName)
    {
        if (to is not null && to < from)
            throw new ValidationException(
                ErrorCodes.Validation,
                $"Поле «{fieldName}»: дата окончания раньше даты начала.");
    }
}
