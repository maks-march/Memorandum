namespace Memorandum.Application.PasswordChange.Models;

public enum ConsumeCodeStatus
{
    Ok,
    Invalid,
    Expired,
    Exhausted,
    NotFound,
}

/// <summary>Результат атомарной проверки кода.</summary>
public sealed record ConsumeCodeResult(ConsumeCodeStatus Status, int AttemptsLeft);
