using Memorandum.Application.Common;
using Memorandum.Application.CurrentUser.Models;

namespace Memorandum.Application.CurrentUser;

public interface ICurrentUserService
{
    /// <summary>Профиль вошедшего пользователя по данным текущего запроса.</summary>
    Task<Result<CurrentUserDto>> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>Правка ФИО, должности и телефона: <c>PATCH /api/v1/me</c>.</summary>
    Task<Result<CurrentUserDto>> UpdateAsync(
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default);
}
