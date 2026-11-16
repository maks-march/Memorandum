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
            return Error.Business(ErrorCodes.InvalidCredentials, "Требуется вход.").ToResult<CurrentUserDto>();

        var user = await users.GetByIdAsync(userId, cancellationToken);

        if (user is null)
            return Error.NotFound("Пользователь", userId).ToResult<CurrentUserDto>();

        if (!user.IsActive)
            return Error.Business(ErrorCodes.UserInactive, "Учётная запись заблокирована.").ToResult<CurrentUserDto>();

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
