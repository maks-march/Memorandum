using Memorandum.Application.Abstractions;
using Memorandum.Application.Common;
using Memorandum.Domain.Common;

namespace Memorandum.Application.CurrentUser;

public sealed class CurrentUserService(
    IUserRepository users,
    IOrganizationRepository organizations,
    ICurrentUser currentUser) : ICurrentUserService
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

        return Result<CurrentUserDto>.Success(new CurrentUserDto(
            user.Id,
            user.Email,
            user.FullName,
            membership?.OrganizationId,
            membership?.OrganizationName,
            membership?.Role));
    }
}
