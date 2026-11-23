using Memorandum.Application.Common;
using Memorandum.Application.Invitations.Models;

namespace Memorandum.Application.Invitations;

public interface IInviteCodeService
{
    /// <summary>
    /// Выдаёт новый код приглашения в организацию вошедшего администратора.
    /// </summary>
    Task<Result<InviteCodeDto>> IssueAsync(
        RequestInviteCodeRequest request,
        CancellationToken cancellationToken = default);
}
