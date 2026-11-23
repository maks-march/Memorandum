namespace Memorandum.Api.Contracts;

/// <summary>Правка профиля: <c>PATCH /api/v1/me</c>. Email здесь не меняется — по нему вход.</summary>
public sealed record UpdateProfileHttpRequest(string? FullName, string? Phone);
