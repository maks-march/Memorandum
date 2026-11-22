using FluentValidation;

namespace Memorandum.Application.PasswordChange.Validators;

/// <summary>
/// Правила второго шага. Валидируется кортеж значений, поэтому имена полей в ответе
/// задаются явно — иначе клиент получит Item1/Item2/Item3 вместо осмысленных ключей.
/// </summary>
public sealed class ConfirmPasswordChangeRequestValidator
    : AbstractValidator<(long UserId, string? Code, string? NewPassword)>
{
    public ConfirmPasswordChangeRequestValidator()
    {
        RuleFor(request => request.UserId)
            .GreaterThan(0).WithMessage("Некорректный идентификатор пользователя.")
            .OverridePropertyName("userId");

        RuleFor(request => request.Code)
            .NotEmpty().WithMessage("Код подтверждения обязателен.")
            .Matches($"^[0-9]{{{PasswordResetCode.Length}}}$")
            .WithMessage($"Код состоит из {PasswordResetCode.Length} цифр.")
            .OverridePropertyName("code");

        RuleFor(request => request.NewPassword)
            .NotEmpty().WithMessage("Пароль обязателен.")
            .MinimumLength(Auth.RegisterRequestValidator.PasswordMinLength)
            .WithMessage($"Пароль должен быть не короче {Auth.RegisterRequestValidator.PasswordMinLength} символов.")
            .MaximumLength(Auth.RegisterRequestValidator.PasswordMaxLength)
            .WithMessage($"Пароль должен быть не длиннее {Auth.RegisterRequestValidator.PasswordMaxLength} символов.")
            .OverridePropertyName("NewPassword");
    }
}
