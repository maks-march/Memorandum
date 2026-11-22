using FluentValidation;
using Memorandum.Application.PasswordChange.Models;
using Memorandum.Domain.Users;

namespace Memorandum.Application.PasswordChange.Validators;

public sealed class RequestPasswordChangeCodeRequestValidator
    : AbstractValidator<RequestPasswordChangeCodeRequest>
{
    public RequestPasswordChangeCodeRequestValidator()
    {
        RuleFor(request => request.Email)
            .NotEmpty().WithMessage("Email обязателен.")
            .MaximumLength(User.EmailMaxLength)
            .EmailAddress().WithMessage("Некорректный email.");
    }
}
