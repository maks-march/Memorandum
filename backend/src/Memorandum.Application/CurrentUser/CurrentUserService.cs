using FluentValidation;
using Memorandum.Application.Abstractions;
using Memorandum.Application.Common;
using Memorandum.Application.CurrentUser.Models;
using Memorandum.Domain.Common;
using Memorandum.Domain.Interfaces;
using Memorandum.Domain.Users;

namespace Memorandum.Application.CurrentUser;

public sealed class CurrentUserService(
    IUserRepository users,
    IOrganizationRepository organizations,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IValidator<UpdateProfileRequest> updateValidator) : ICurrentUserService
{
    public async Task<Result<CurrentUserDto>> GetAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.Id is not { } userId)
            return Result<CurrentUserDto>.Failure(
                Error.Business(ErrorCodes.InvalidCredentials, "Требуется вход."));

        var user = await users.GetByIdAsync(userId, cancellationToken);

        if (user is null)
            return Result<CurrentUserDto>.Failure(Error.NotFound("Пользователь", userId));

        if (!user.IsActive)
            return Result<CurrentUserDto>.Failure(
                Error.Business(ErrorCodes.UserInactive, "Учётная запись заблокирована."));

        var membership = await organizations.GetActiveMembershipAsync(userId, cancellationToken);

        return Result<CurrentUserDto>.Success(ToDto(user, membership));
    }

    public async Task<Result<CurrentUserDto>> UpdateAsync(
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result<CurrentUserDto>.Failure(validation.ToError());

        if (currentUser.Id is not { } userId)
            return Result<CurrentUserDto>.Failure(
                Error.Business(ErrorCodes.InvalidCredentials, "Требуется вход."));

        var user = await users.GetForUpdateAsync(userId, cancellationToken);

        if (user is null)
            return Result<CurrentUserDto>.Failure(Error.NotFound("Пользователь", userId));

        if (!user.IsActive)
            return Result<CurrentUserDto>.Failure(
                Error.Business(ErrorCodes.UserInactive, "Учётная запись заблокирована."));

        // Время берём один раз на операцию: им же помечается аудит сохранения.
        var now = timeProvider.GetUtcNow();
        user.ChangeProfile(request.FullName, request.Phone, now.UtcDateTime, user.Email);

        var saved = await unitOfWork.TrySaveAsync(cancellationToken);
        if (saved.IsFailure)
            return Result<CurrentUserDto>.Failure(saved.Error);

        var membership = await organizations.GetActiveMembershipAsync(userId, cancellationToken);
        return Result<CurrentUserDto>.Success(ToDto(user, membership));
    }

    private static CurrentUserDto ToDto(User user, ActiveMembership? membership) =>
        new(
            user.Id,
            user.Email,
            user.FullName,
            user.Phone,
            membership?.Position,
            membership?.OrganizationId,
            membership?.OrganizationName,
            membership?.Role);
}
