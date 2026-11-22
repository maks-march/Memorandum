using FluentValidation;
using FluentValidation.Results;
using Memorandum.Application.Abstractions;

namespace Memorandum.Tests.Application;

/// <summary>Хешер без AspNetCore Identity: хеш предсказуем, проверка — сравнение.</summary>
public sealed class FakePasswordHasher : IPasswordHasher
{
    public const string Prefix = "hash:";

    public PasswordVerificationResult Result { get; set; } = PasswordVerificationResult.Success;

    public string Hash(string password) => Prefix + password;

    public PasswordVerificationResult Verify(string passwordHash, string password) =>
        Result == PasswordVerificationResult.SuccessRehashNeeded
            ? PasswordVerificationResult.SuccessRehashNeeded
            : passwordHash == Prefix + password ? PasswordVerificationResult.Success : PasswordVerificationResult.Failed;
}

/// <summary>Текущий пользователь для сценариев: анонимный или заданный.</summary>
public sealed class FakeCurrentUser(long? id = null, string? name = null) : ICurrentUser
{
    public long? Id { get; } = id;

    public string? Name { get; } = name;
}

/// <summary>
/// Валидатор, который всё пропускает: нужен, чтобы проверить трансляцию доменных исключений,
/// не подменяя правила формы.
/// </summary>
public sealed class AlwaysValidValidator<T> : IValidator<T>
{
    public ValidationResult Validate(T instance) => new();

    public Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellation = default) =>
        Task.FromResult(new ValidationResult());

    public IValidatorDescriptor CreateDescriptor() => throw new NotSupportedException();

    public bool CanValidateInstancesOfType(Type type) => true;

    public ValidationResult Validate(IValidationContext context) => new();

    public Task<ValidationResult> ValidateAsync(IValidationContext context, CancellationToken cancellation = default) =>
        Task.FromResult(new ValidationResult());
}
