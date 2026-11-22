using FluentValidation;
using Memorandum.Domain.Organizations;
using Memorandum.Domain.Users;

namespace Memorandum.Application.Auth;

/// <summary>
/// Правила формы. Проверки, которым нужны данные (дубликат email), остаются в сервисе.
/// Пароль не обрезается и не нормализуется: что пришло, то и хешируется.
/// </summary>
public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128;

    public RegisterRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty().WithMessage("Email обязателен.")
            .MaximumLength(User.EmailMaxLength).WithMessage($"Email не должен быть длиннее {User.EmailMaxLength} символов.")
            .EmailAddress().WithMessage("Некорректный email.");

        RuleFor(request => request.Password)
            .NotEmpty().WithMessage("Пароль обязателен.")
            .MinimumLength(PasswordMinLength).WithMessage($"Пароль должен быть не короче {PasswordMinLength} символов.")
            .MaximumLength(PasswordMaxLength).WithMessage($"Пароль должен быть не длиннее {PasswordMaxLength} символов.");

        RuleFor(request => request.FullName)
            .NotEmpty().WithMessage("ФИО обязательно.")
            .MaximumLength(User.FullNameMaxLength).WithMessage($"ФИО не должно быть длиннее {User.FullNameMaxLength} символов.");

        RuleFor(request => request.OrgName)
            .MaximumLength(Organization.NameMaxLength)
            .WithMessage($"Название организации не должно быть длиннее {Organization.NameMaxLength} символов.")
            .When(request => request.OrgName is not null);
    }
}
