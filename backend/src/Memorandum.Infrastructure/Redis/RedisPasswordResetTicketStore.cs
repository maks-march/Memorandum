using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Memorandum.Application.Abstractions;
using Memorandum.Application.PasswordChange;
using Memorandum.Application.PasswordChange.Models;

namespace Memorandum.Infrastructure.Redis;

/// <summary>
/// Тикеты сброса пароля в Redis. Проверка кода — Lua-скрипт: чтение, сравнение,
/// декремент и удаление внутри одной атомарной операции Redis.
/// </summary>
public sealed class RedisPasswordResetTicketStore : IPasswordResetTicketStore
{
    private const string TicketPrefix = "pwd:reset:";
    private const string CooldownPrefix = "pwd:cooldown:";

    private const string ConsumeScript = """
        local raw = redis.call('GET', KEYS[1])
        if not raw then return { 'NOT_FOUND', 0 } end

        local ticket = cjson.decode(raw)
        local now = tonumber(ARGV[2])

        if tonumber(ticket.expiresAt) <= now then
            redis.call('DEL', KEYS[1])
            return { 'EXPIRED', 0 }
        end

        if ticket.codeHash == ARGV[1] then
            redis.call('DEL', KEYS[1])
            return { 'OK', tonumber(ticket.attemptsLeft) }
        end

        ticket.attemptsLeft = tonumber(ticket.attemptsLeft) - 1
        if ticket.attemptsLeft <= 0 then
            redis.call('DEL', KEYS[1])
            return { 'EXHAUSTED', 0 }
        end

        -- KEEPTTL: тикет не должен жить дольше исходного срока
        redis.call('SET', KEYS[1], cjson.encode(ticket), 'KEEPTTL')
        return { 'INVALID', ticket.attemptsLeft }
        """;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Вид тикета в Redis. Сроки храним числами: cjson в Lua не умеет разбирать ISO-строки,
    /// а сравнивать expiresAt с текущим временем приходится внутри скрипта.
    /// </summary>
    private sealed record TicketPayload(
        string CodeHash,
        int AttemptsLeft,
        long ExpiresAt,
        long IssuedAt,
        string Email)
    {
        public static TicketPayload From(PasswordResetTicket ticket) => new(
            ticket.CodeHash,
            ticket.AttemptsLeft,
            ticket.ExpiresAt.ToUnixTimeSeconds(),
            ticket.IssuedAt.ToUnixTimeSeconds(),
            ticket.Email);
    }

    private readonly IDatabase _database;
    private readonly TimeProvider _timeProvider;
    private readonly PasswordResetOptions _options;

    public RedisPasswordResetTicketStore(
        IConnectionMultiplexer redis,
        TimeProvider timeProvider,
        IOptions<PasswordResetOptions> options)
    {
        _database = redis.GetDatabase();
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    public Task<bool> TryStartCooldownAsync(
        long userId,
        TimeSpan cooldown,
        CancellationToken cancellationToken = default) =>
        _database.StringSetAsync(TicketKey(CooldownPrefix, userId), "1", cooldown, When.NotExists);

    public Task SaveAsync(
        long userId,
        PasswordResetTicket ticket,
        TimeSpan timeToLive,
        CancellationToken cancellationToken = default) =>
        _database.StringSetAsync(
            TicketKey(TicketPrefix, userId),
            JsonSerializer.Serialize(TicketPayload.From(ticket), Json),
            timeToLive);

    public async Task<ConsumeCodeResult> ConsumeCodeAsync(
        long userId,
        string codeHash,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().ToUnixTimeSeconds();

        var result = await _database.ScriptEvaluateAsync(
            ConsumeScript,
            [TicketKey(TicketPrefix, userId)],
            [codeHash, now]);

        var values = (RedisResult[])result!;
        var status = values[0].ToString();
        var attemptsLeft = (int)values[1];

        return new ConsumeCodeResult(
            status switch
            {
                "OK" => ConsumeCodeStatus.Ok,
                "EXPIRED" => ConsumeCodeStatus.Expired,
                "EXHAUSTED" => ConsumeCodeStatus.Exhausted,
                "INVALID" => ConsumeCodeStatus.Invalid,
                _ => ConsumeCodeStatus.NotFound,
            },
            attemptsLeft);
    }

    /// <summary>Остаток срока и содержимое тикета — для диагностики и тестов хранилища.</summary>
    public async Task<(string? Payload, TimeSpan? TimeToLive)> InspectAsync(long userId)
    {
        var key = TicketKey(TicketPrefix, userId);
        var payload = await _database.StringGetAsync(key);
        var ttl = await _database.KeyTimeToLiveAsync(key);

        return (payload.HasValue ? payload.ToString() : null, ttl);
    }

    private static string TicketKey(string prefix, long userId) =>
        prefix + userId.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
