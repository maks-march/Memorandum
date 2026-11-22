namespace Memorandum.Application.PasswordChange.Models;

/// <summary>Шаг 1: запрос кода на email.</summary>
public sealed record RequestPasswordChangeCodeRequest(string? Email);
