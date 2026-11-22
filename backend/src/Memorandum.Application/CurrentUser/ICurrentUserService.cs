using Memorandum.Application.Common;

namespace Memorandum.Application.CurrentUser;

public interface ICurrentUserService
{
    /// <summary>Профиль вошедшего пользователя по данным текущего запроса.</summary>
    Task<Result<CurrentUserDto>> GetAsync(CancellationToken cancellationToken = default);
}
