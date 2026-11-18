namespace Memorandum.Api.Contracts;

/// <summary>Тело <c>POST /api/v1/auth/login</c>.</summary>
public sealed record LoginHttpRequest(string? Email, string? Password);
