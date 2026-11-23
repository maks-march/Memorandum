namespace Memorandum.Api.Contracts;

/// <summary>
/// Тело <c>POST /api/v1/auth/accept-invite</c>: регистрация сотрудника по коду.
/// Организацию и роль задаёт код.
/// </summary>
public sealed record AcceptInviteHttpRequest(string? Code, string? Email, string? Password, string? FullName);
