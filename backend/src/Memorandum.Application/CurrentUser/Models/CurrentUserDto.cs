using Memorandum.Domain.Organizations;

namespace Memorandum.Application.CurrentUser;

/// <summary>Профиль вошедшего пользователя: <c>GET /api/v1/me</c>.</summary>
/// <param name="OrganizationId">Текущая организация; <c>null</c>, если пользователь ещё не состоит ни в одной.</param>
/// <param name="Role">Роль в текущей организации; <c>null</c> вместе с организацией.</param>
public sealed record CurrentUserDto(
    long Id,
    string Email,
    string FullName,
    long? OrganizationId,
    string? OrganizationName,
    OrganizationRole? Role);
