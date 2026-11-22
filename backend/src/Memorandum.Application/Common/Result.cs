namespace Memorandum.Application.Common;

/// <summary>
/// Результат сценария. Ожидаемые ошибки (валидация,404,409,401) возвращаются как Failure;
/// неожиданные исключения не перехватываются и становятся 500.
/// </summary>
public sealed class Result<T>
{
    private readonly T? _value;
    private readonly Error? _error;

    private Result(bool isSuccess, T? value, Error? error)
    {
        IsSuccess = isSuccess;
        _value = value;
        _error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Нет значения: операция завершилась ошибкой.");

    public Error Error => !IsSuccess
        ? _error!
        : throw new InvalidOperationException("Нет ошибки: операция успешна.");

    public static Result<T> Success(T value) => new(true, value, null);

    public static Result<T> Failure(Error error) => new(false, default, error);
}

/// <summary>Результат операции без значения.</summary>
public sealed class Result
{
    private readonly Error? _error;

    private Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        _error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error => !IsSuccess
        ? _error!
        : throw new InvalidOperationException("Нет ошибки: операция успешна.");

    public static Result Success() => new(true, null);

    public static Result Failure(Error error) => new(false, error);
}
