namespace Memorandum.Application.PasswordChange.Models;

/// <summary>Тикет в хранилище. Код хранится только как хеш: в Redis его в открытом виде быть не должно.</summary>
public sealed record PasswordResetTicket(
    string CodeHash,
    int AttemptsLeft,
    DateTimeOffset ExpiresAt,
    DateTimeOffset IssuedAt,
    string Email);
