using Memorandum.Application.Abstractions;
using Memorandum.Application.PasswordChange.Models;

namespace Memorandum.Tests.Application;

/// <summary>Тикеты в памяти: та же семантика, что у Lua-скрипта в Redis.</summary>
public sealed class FakeTicketStore : IPasswordResetTicketStore
{
    private readonly Dictionary<long, PasswordResetTicket> _tickets = [];
    private readonly HashSet<long> _cooldowns = [];
    private readonly HashSet<long> _expired = [];

    public PasswordResetTicket? SavedTicket { get; private set; }

    public Task<bool> TryStartCooldownAsync(long userId, TimeSpan cooldown, CancellationToken cancellationToken = default)
    {
        if (!_cooldowns.Add(userId))
            return Task.FromResult(false);

        return Task.FromResult(true);
    }

    public Task SaveAsync(long userId, PasswordResetTicket ticket, TimeSpan timeToLive, CancellationToken cancellationToken = default)
    {
        _tickets[userId] = ticket;
        SavedTicket = ticket;
        return Task.CompletedTask;
    }

    public Task<ConsumeCodeResult> ConsumeCodeAsync(long userId, string codeHash, CancellationToken cancellationToken = default)
    {
        if (!_tickets.TryGetValue(userId, out var ticket))
            return Task.FromResult(new ConsumeCodeResult(ConsumeCodeStatus.NotFound, 0));

        if (_expired.Contains(userId))
        {
            _tickets.Remove(userId);
            SavedTicket = null;
            return Task.FromResult(new ConsumeCodeResult(ConsumeCodeStatus.Expired, 0));
        }

        if (ticket.CodeHash == codeHash)
        {
            _tickets.Remove(userId);
            SavedTicket = null;
            return Task.FromResult(new ConsumeCodeResult(ConsumeCodeStatus.Ok, ticket.AttemptsLeft));
        }

        var attemptsLeft = ticket.AttemptsLeft - 1;

        if (attemptsLeft <= 0)
        {
            _tickets.Remove(userId);
            SavedTicket = null;
            return Task.FromResult(new ConsumeCodeResult(ConsumeCodeStatus.Exhausted, 0));
        }

        _tickets[userId] = ticket with { AttemptsLeft = attemptsLeft };
        SavedTicket = _tickets[userId];
        return Task.FromResult(new ConsumeCodeResult(ConsumeCodeStatus.Invalid, attemptsLeft));
    }

    /// <summary>Истечение срока: в Redis это TTL и сравнение expiresAt в Lua, здесь — тест.</summary>
    public void Expire(long userId) => _expired.Add(userId);

    public void ResetCooldown(long userId) => _cooldowns.Remove(userId);
}

/// <summary>Письма складываются в список: тест видит код, не читая логов.</summary>
public sealed class FakeEmailSender : IEmailSender
{
    public List<(string Email, long UserId, string Code, DateTimeOffset ExpiresAt)> Sent { get; } = [];

    public Task SendPasswordResetCodeAsync(
        string email,
        long userId,
        string code,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        Sent.Add((email, userId, code, expiresAt));
        return Task.CompletedTask;
    }
}
