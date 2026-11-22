namespace Memorandum.Application.PasswordChange.Models;

/// <summary>Шаг 2: подтверждение кодом и новый пароль.</summary>
public sealed record ConfirmPasswordChangeRequest(string? NewPassword);
