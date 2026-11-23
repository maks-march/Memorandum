using Memorandum.Domain.Organizations;

namespace Memorandum.Api.Contracts;

/// <summary>
/// Тело <c>POST /api/v1/auth/invite-code</c>. Организация не передаётся:
/// она берётся из участия вошедшего администратора.
/// </summary>
/// <param name="Role">Роль приглашённого: Editor или Viewer (администратора по коду не выдают).</param>
public sealed record RequestInviteCodeHttpRequest(OrganizationRole Role);
