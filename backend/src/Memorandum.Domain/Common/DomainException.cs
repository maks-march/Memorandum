namespace Memorandum.Domain.Common;

/// <summary>
/// Ожидаемая ошибка домена. Домен бросает, Application переводит в Failure,
/// Api отдаёт ProblemDetails — до HTTP как 500 такое исключение доходить не должно.
/// </summary>
public abstract class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

/// <summary>Объект не найден или недоступен текущему пользователю.</summary>
public sealed class NotFoundException(string code, string message) : DomainException(code, message);

/// <summary>Состояние не позволяет выполнить операцию: дубликат, последний администратор, деактивированная организация.</summary>
public sealed class ConflictException(string code, string message) : DomainException(code, message);

/// <summary>Нарушено правило заполнения значения: обязательность, длина, формат.</summary>
public sealed class ValidationException(string code, string message) : DomainException(code, message);
