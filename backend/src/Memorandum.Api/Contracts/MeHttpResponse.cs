using Memorandum.Application.CurrentUser;
using Memorandum.Domain.Organizations;

namespace Memorandum.Api.Contracts;

/// <summary>
/// Профиль вошедшего пользователя: <c>GET /api/v1/me</c>, <c>PATCH /api/v1/me</c>
/// и ответ на регистрацию. Секретов (пароль, хеш, security stamp) здесь нет by design.
/// </summary>
public sealed record MeHttpResponse(
    long Id,
    string Email,
    string FullName,
    string? Phone,
    string? Position,
    long? OrganizationId,
    string? OrganizationName,
    OrganizationRole? Role)
{
    public static MeHttpResponse From(CurrentUserDto dto) =>
        new(dto.Id, dto.Email, dto.FullName, dto.Phone, dto.Position,
            dto.OrganizationId, dto.OrganizationName, dto.Role);
}
