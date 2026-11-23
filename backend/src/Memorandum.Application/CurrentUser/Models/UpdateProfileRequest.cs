namespace Memorandum.Application.CurrentUser.Models;

/// <summary>Правка личных данных профиля: <c>PATCH /api/v1/me</c>.</summary>
public sealed record UpdateProfileRequest(string? FullName, string? Phone);
