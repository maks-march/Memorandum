using FluentValidation;
using Microsoft.Extensions.Options;
using Memorandum.Application.Abstractions;
using Memorandum.Application.Common;
using Memorandum.Application.Invitations.Models;
using Memorandum.Domain.Common;
using Memorandum.Domain.Interfaces;
using Memorandum.Domain.Organizations;

namespace Memorandum.Application.Invitations;

/// <summary>
/// Выдача кодов приглашения. Код показывает администратору организации и передаёт
/// новому сотруднику любым каналом; тот регистрируется с ним и попадает в организацию
/// с заранее выбранной ролью.
/// </summary>
public sealed class InviteCodeService(
    IOrganizationRepository organizations,
    IInviteCodeStore store,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IOptions<InvitationOptions> options,
    IValidator<RequestInviteCodeRequest> validator) : IInviteCodeService
{
    public async Task<Result<InviteCodeDto>> IssueAsync(
        RequestInviteCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result<InviteCodeDto>.Failure(validation.ToError());

        if (currentUser.Id is not { } userId)
            return Result<InviteCodeDto>.Failure(
                Error.Business(ErrorCodes.InvalidCredentials, "Требуется вход."));

        // Организация берётся из участия, а не из запроса: код нельзя выписать
        // в чужую организацию, подставив её id.
        var membership = await organizations.GetActiveMembershipAsync(userId, cancellationToken);

        if (membership is null)
            return Result<InviteCodeDto>.Failure(
                Error.Business(ErrorCodes.Forbidden, "Пользователь не состоит в организации."));

        if (membership.Role != OrganizationRole.OrgAdmin)
            return Result<InviteCodeDto>.Failure(
                Error.Business(ErrorCodes.Forbidden, "Код приглашения выдаёт администратор организации."));

        var organization = await organizations.GetByIdAsync(membership.OrganizationId, cancellationToken);

        if (organization is null)
            return Result<InviteCodeDto>.Failure(
                Error.NotFound("Организация", membership.OrganizationId));

        if (!organization.IsActive)
            return Result<InviteCodeDto>.Failure(
                Error.Business(ErrorCodes.OrganizationInactive, "Организация деактивирована."));

        var now = timeProvider.GetUtcNow();
        var expiresAt = now.Add(options.Value.CodeTimeToLive);
        var code = InviteCode.Generate();

        var entry = new InviteCodeEntry(
            OrganizationId: organization.Id,
            CodeHash: InviteCode.Hash(options.Value.CodePepper, code),
            Role: request.Role,
            ExpiresAt: expiresAt);

        try
        {
            await store.SaveAsync(entry, code, options.Value.CodeTimeToLive, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Ключовое хранилище недоступно — это 503, а не ошибка клиента.
            return Result<InviteCodeDto>.Failure(Error.Business(
                ErrorCodes.StorageUnavailable,
                "Сервис временно недоступен: не удалось сохранить код приглашения."));
        }

        return Result<InviteCodeDto>.Success(
            new InviteCodeDto(organization.Id, organization.Name, code, entry.Role, expiresAt));
    }
}
