using FluentValidation;

namespace Memorandum.Application.Auth;

/// <summary>Форма входа: пустые поля отбиваются до обращения к БД.</summary>
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().WithMessage("Email обязателен.");
        RuleFor(request => request.Password).NotEmpty().WithMessage("Пароль обязателен.");
    }
}
