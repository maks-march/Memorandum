using FluentValidation;
using Memorandum.Application.CurrentUser.Models;
using Memorandum.Domain.Users;

namespace Memorandum.Application.CurrentUser.Validators;

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(request => request.FullName)
            .NotEmpty().WithMessage("ФИО обязательно для заполнения.")
            .MaximumLength(User.FullNameMaxLength)
            .WithMessage($"ФИО не должно быть длиннее {User.FullNameMaxLength} символов.");

        RuleFor(request => request.Phone)
            .MaximumLength(User.PhoneMaxLength)
            .WithMessage($"Телефон не должен быть длиннее {User.PhoneMaxLength} символов.")
            .Matches(@"^[0-9+()\-\s]*$")
            .WithMessage("Телефон может содержать только цифры, пробелы и символы + ( ) -.");
    }
}
