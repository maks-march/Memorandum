using Memorandum.Domain.Organizations;

namespace Memorandum.Application.Invitations.Models;

/// <summary>
/// Запрос кода приглашения: <c>POST /api/v1/auth/invite-code</c>.
/// Других полей нет: организация берётся из участия вошедшего пользователя.
/// </summary>
/// <param name="Role">Какую роль получит приглашённый; по умолчанию — редактор.</param>
public sealed record RequestInviteCodeRequest(OrganizationRole Role);
