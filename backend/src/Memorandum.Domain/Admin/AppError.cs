using Memorandum.Domain.Common;

namespace Memorandum.Domain.Admin;

/// <summary>Уровень ошибки приложения.</summary>
public enum AppErrorLevel
{
    Warning = 1,
    Error = 2,
    Fatal = 3,
}

/// <summary>
/// Ошибка приложения для экрана <c>/admin/errors</c>: что случилось и по какому запросу.
/// </summary>
/// <remarks>
/// Неизменяемая запись журнала: создаётся один раз обработчиком исключений.
/// </remarks>
public sealed class AppError : AuditableEntity
{
    public const int MessageMaxLength = 2000;
    public const int PathMaxLength = 500;
    public const int StackTraceMaxLength = 8000;
    public const int CodeMaxLength = 100;
    public const int TraceIdMaxLength = 64;

    private AppError()
    {
    }

    public AppErrorLevel Level { get; private set; }

    /// <summary>Код ошибки домена или <c>INFRASTRUCTURE</c> для непредвиденных исключений.</summary>
    public string Code { get; private set; } = null!;

    public string Message { get; private set; } = null!;

    public string? StackTrace { get; private set; }

    public string? TraceId { get; private set; }

    public string? Path { get; private set; }

    public long? UserId { get; private set; }

    public long? OrganizationId { get; private set; }

    /// <summary>Повторяющиеся ошибки не растят таблицу: счётчик увеличивается.</summary>
    public int RepeatCount { get; private set; } = 1;

    public DateTime? LastOccurredAt { get; private set; }

    public static AppError Create(
        AppErrorLevel level,
        string? code,
        string? message,
        DateTime utcNow,
        string? path = null,
        string? traceId = null,
        string? stackTrace = null,
        long? userId = null,
        long? organizationId = null,
        string? createdBy = null)
    {
        var error = new AppError
        {
            Level = level,
            Code = Guard.Required(code, "Код ошибки", CodeMaxLength),
            Message = Guard.Required(message, "Текст ошибки", MessageMaxLength),
            Path = Guard.Optional(path, "Путь запроса", PathMaxLength),
            TraceId = Guard.Optional(traceId, "TraceId", TraceIdMaxLength),
            StackTrace = Guard.Optional(stackTrace, "Стек вызовов", StackTraceMaxLength),
            UserId = userId,
            OrganizationId = organizationId,
            LastOccurredAt = utcNow,
        };

        error.MarkCreated(utcNow, createdBy);
        return error;
    }

    /// <summary>То же исключение повторилось: увеличиваем счётчик вместо новой записи.</summary>
    public void Repeat(DateTime utcNow)
    {
        RepeatCount++;
        LastOccurredAt = utcNow;
    }
}
