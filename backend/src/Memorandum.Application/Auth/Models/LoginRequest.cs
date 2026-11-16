namespace Memorandum.Application.Auth;

/// <summary>Запрос входа: <c>POST /api/v1/auth/login</c>.</summary>
public sealed record LoginRequest(string? Email, string? Password);
