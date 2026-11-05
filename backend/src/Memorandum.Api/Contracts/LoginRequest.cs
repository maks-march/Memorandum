namespace Memorandum.Api.Contracts;

/// <summary>Вход. Роли и cookie — как в реальном /api/v1/auth/login.</summary>
public sealed record LoginRequest(string Email, string Password);
