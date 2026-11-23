using FluentValidation;
using Memorandum.Application.Invitations.Models;
using Memorandum.Domain.Organizations;

namespace Memorandum.Application.Invitations.Validators;

public sealed class RequestInviteCodeRequestValidator : AbstractValidator<RequestInviteCodeRequest>
{
    public RequestInviteCodeRequestValidator()
    {
        // Администратора по коду не назначают: иначе любой, кто узнал код,
        // получал бы полные права на организацию.
        RuleFor(request => request.Role)
            .Must(Organization.CanInviteWithRole)
            .WithMessage("По коду приглашения можно выдать роль Editor или Viewer.");
    }
}
