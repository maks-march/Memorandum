using System.Text.RegularExpressions;

namespace Memorandum.Domain.Common;

/// <summary>Проверки входных значений сущностей. Нарушение — <see cref="DomainException"/>.</summary>
public static partial class Guard
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

    /// <summary>Значение из фиксированного набора; сравнение без учёта регистра.</summary>
    public static string InSet(string? value, string fieldName, IReadOnlyCollection<string> allowed)
    {
        var normalized = Required(value, fieldName);

        if (!allowed.Any(item => string.Equals(item, normalized, StringComparison.OrdinalIgnoreCase)))
            throw new ValidationException(
                ErrorCodes.InvalidFormat,
                $"Поле «{fieldName}»: допустимые значения — {string.Join(", ", allowed)}.");

        return allowed.First(item => string.Equals(item, normalized, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Формат по регулярному выражению: ISO-код, ИНН, СНИЛС, НРС и подобное.</summary>
    public static string Matches(string? value, string fieldName, Regex pattern, string expected, int? maxLength = null)
    {
        var normalized = Required(value, fieldName, maxLength);

        if (!pattern.IsMatch(normalized))
            throw new ValidationException(
                ErrorCodes.InvalidFormat,
                $"Поле «{fieldName}» должно соответствовать формату: {expected}.");

        return normalized;
    }

    /// <summary>Число обязано быть неотрицательным.</summary>
    public static int NotNegative(int value, string fieldName)
    {
        if (value < 0)
            throw new ValidationException(
                ErrorCodes.Validation,
                $"Поле «{fieldName}» не может быть отрицательным.");

        return value;
    }

    /// <summary>Размер файла: не отрицательный.</summary>
    public static long NotNegative(long value, string fieldName)
    {
        if (value < 0)
            throw new ValidationException(
                ErrorCodes.Validation,
                $"Поле «{fieldName}» не может быть отрицательным.");

        return value;
    }

    /// <summary>Набор кодов: каждый должен входить в допустимые, повторов нет.</summary>
    public static IReadOnlyCollection<string> UniqueInSet(
        IEnumerable<string> values,
        string fieldName,
        IReadOnlyCollection<string> allowed)
    {
        var normalized = values.Select(value => InSet(value, fieldName, allowed)).ToArray();

        if (normalized.Distinct(StringComparer.OrdinalIgnoreCase).Count() != normalized.Length)
            throw new ValidationException(ErrorCodes.Validation, $"Поле «{fieldName}» содержит повторы.");

        return normalized;
    }

    /// <summary>ИНН: 10 цифр у организации, 12 у ИП.</summary>
    [GeneratedRegex(@"^(\d{10}|\d{12})$")]
    public static partial Regex Inn();

    /// <summary>КПП: 9 цифр.</summary>
    [GeneratedRegex(@"^\d{9}$")]
    public static partial Regex Kpp();

    /// <summary>ОГРН: 13 цифр у организации, 15 у ИП.</summary>
    [GeneratedRegex(@"^(\d{13}|\d{15})$")]
    public static partial Regex Ogrn();

    /// <summary>СНИЛС: 11 цифр, допускаются разделители.</summary>
    [GeneratedRegex(@"^\d{3}-?\d{3}-?\d{3}[- ]?\d{2}$")]
    public static partial Regex Snils();

    /// <summary>Номер в НРС НОПРИЗ: «П-123456», «ПИ-123456», «И-123456».</summary>
    [GeneratedRegex(@"^(П|ПИ|И)-\d{6}$")]
    public static partial Regex NrsNopriz();

    /// <summary>Код ОКТМО: 8 или 11 цифр.</summary>
    [GeneratedRegex(@"^(\d{8}|\d{11})$")]
    public static partial Regex Oktmo();

    /// <summary>Код субъекта РФ: две цифры.</summary>
    [GeneratedRegex(@"^\d{2}$")]
    public static partial Regex RegionCode();

    /// <summary>Хеш SHA-256 в hex.</summary>
    [GeneratedRegex(@"^[0-9a-fA-F]{64}$")]
    public static partial Regex Sha256();
}
