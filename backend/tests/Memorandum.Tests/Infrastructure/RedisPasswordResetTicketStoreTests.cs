using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Memorandum.Application.PasswordChange;
using Memorandum.Application.PasswordChange.Models;
using Memorandum.Infrastructure.Redis;

namespace Memorandum.Tests.Infrastructure;

/// <summary>
/// Тесты хранилища на живом Redis.
/// </summary>
[Trait("Category", "Redis")]
public sealed class RedisPasswordResetTicketStoreTests : IAsyncLifetime
{
    private readonly PasswordResetOptions _options = new() { CodePepper = "test-pepper" };

    private IConnectionMultiplexer? _redis;

    public async Task InitializeAsync() => _redis = await RedisTestServer.ConnectAsync();



    public async Task DisposeAsync()
    {
        if (_redis is null)
            throw new InvalidOperationException("Соединение с Redis не открыто.");

        await _redis.GetDatabase().ExecuteAsync("FLUSHDB");
        await _redis.DisposeAsync();
    }

    private RedisPasswordResetTicketStore CreateStore(TimeProvider? timeProvider = null) =>
        new(
            _redis!,
            timeProvider ?? TimeProvider.System,
            Options.Create(_options));

    private static PasswordResetTicket NewTicket(long userId, string code, int attempts = 3, DateTimeOffset? issuedAt = null)
    {
        var now = issuedAt ?? DateTimeOffset.UtcNow;

        return new PasswordResetTicket(
            PasswordResetCode.Hash("test-pepper", userId, code),
            attempts,
            now.AddMinutes(15),
            now,
            "a@b.ru");
    }

    [Fact]
    public async Task Ticket_stores_only_hash()
    {
        if (_redis is null)
            throw new InvalidOperationException("Соединение с Redis не открыто.");

        const long userId = 1;
        var store = CreateStore();
        await store.SaveAsync(userId, NewTicket(userId, "123456"), TimeSpan.FromMinutes(15));

        var (payload, _) = await store.InspectAsync(userId);

        Assert.NotNull(payload);
        Assert.Contains("codeHash", payload, StringComparison.Ordinal);
        Assert.DoesNotContain("123456", payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Ttl_is_fifteen_minutes()
    {
        if (_redis is null)
            throw new InvalidOperationException("Соединение с Redis не открыто.");

        const long userId = 2;
        var store = CreateStore();
        await store.SaveAsync(userId, NewTicket(userId, "123456"), TimeSpan.FromMinutes(15));

        var (_, ttl) = await store.InspectAsync(userId);

        Assert.NotNull(ttl);
        Assert.InRange(ttl!.Value, TimeSpan.FromMinutes(14), TimeSpan.FromMinutes(15));
    }

    [Fact]
    public async Task Correct_code_deletes_ticket()
    {
        if (_redis is null)
            throw new InvalidOperationException("Соединение с Redis не открыто.");

        const long userId = 3;
        var store = CreateStore();
        var ticket = NewTicket(userId, "123456");
        await store.SaveAsync(userId, ticket, TimeSpan.FromMinutes(15));

        var result = await store.ConsumeCodeAsync(userId, ticket.CodeHash);

        Assert.Equal(ConsumeCodeStatus.Ok, result.Status);
        Assert.Null((await store.InspectAsync(userId)).Payload);
    }

    [Fact]
    public async Task Unknown_ticket_is_reported_as_not_found()
    {
        if (_redis is null)
            throw new InvalidOperationException("Соединение с Redis не открыто.");

        var result = await CreateStore().ConsumeCodeAsync(42, "hash");

        Assert.Equal(ConsumeCodeStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Wrong_code_decrements_attempts_atomically()
    {
        if (_redis is null)
            throw new InvalidOperationException("Соединение с Redis не открыто.");

        const long userId = 4;
        var store = CreateStore();
        await store.SaveAsync(userId, NewTicket(userId, "123456"), TimeSpan.FromMinutes(15));

        var results = await Task.WhenAll(Enumerable.Range(0, 10)
            .Select(_ => store.ConsumeCodeAsync(userId, "wrong-hash")));

        var invalid = results.Where(result => result.Status == ConsumeCodeStatus.Invalid).ToArray();

        // Три попытки, дальше тикет удалён: параллельные запросы не могут «выиграть» больше.
        Assert.True(invalid.Length <= 2, $"Неудачных проверок больше ожидаемого: {invalid.Length}");
        Assert.Contains(results, result => result.Status == ConsumeCodeStatus.Exhausted);
        Assert.Null((await store.InspectAsync(userId)).Payload);
    }

    [Fact]
    public async Task Expired_ticket_is_reported_as_expired()
    {
        if (_redis is null)
            throw new InvalidOperationException("Соединение с Redis не открыто.");

        const long userId = 5;
        var now = DateTimeOffset.UtcNow;
        var store = CreateStore(new TestTimeProvider(now.AddMinutes(20)));
        var ticket = NewTicket(userId, "123456", issuedAt: now);
        await store.SaveAsync(userId, ticket, TimeSpan.FromMinutes(15));

        var result = await store.ConsumeCodeAsync(userId, ticket.CodeHash);

        Assert.Equal(ConsumeCodeStatus.Expired, result.Status);
    }

    [Fact]
    public async Task Cooldown_blocks_second_request()
    {
        if (_redis is null)
            throw new InvalidOperationException("Соединение с Redis не открыто.");

        const long userId = 6;
        var store = CreateStore();

        Assert.True(await store.TryStartCooldownAsync(userId, TimeSpan.FromSeconds(60)));
        Assert.False(await store.TryStartCooldownAsync(userId, TimeSpan.FromSeconds(60)));
    }
}
