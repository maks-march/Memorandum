using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Memorandum.Application.Invitations;
using Memorandum.Domain.Organizations;
using Memorandum.Infrastructure.Redis;

namespace Memorandum.Tests.Infrastructure;

/// <summary>
/// Коды приглашения на живом Redis: TTL сутки, одноразовость, вытеснение предыдущего.
/// Набор не должен идти параллельно с другими Redis-тестами (см. xunit.runner.json).
/// </summary>
[Trait("Category", "Redis")]
public sealed class RedisInviteCodeStoreTests : IAsyncLifetime
{
    private static readonly TimeSpan Day = TimeSpan.FromDays(1);

    private readonly InvitationOptions _options = new() { CodePepper = "test-pepper" };

    private IConnectionMultiplexer? _redis;

    public async Task InitializeAsync() => _redis = await RedisTestServer.ConnectAsync();

    public async Task DisposeAsync()
    {
        if (_redis is null)
            throw new InvalidOperationException("Соединение с Redis не открыто.");

        await _redis.GetDatabase().ExecuteAsync("FLUSHDB");
        await _redis.DisposeAsync();
    }

    private RedisInviteCodeStore CreateStore(TimeProvider? timeProvider = null) =>
        new(_redis!, timeProvider ?? TimeProvider.System, Options.Create(_options));

    private InviteCodeEntry Entry(long organizationId, string code, DateTimeOffset? expiresAt = null) =>
        new(organizationId,
            InviteCode.Hash(_options.CodePepper, code),
            OrganizationRole.Editor,
            expiresAt ?? DateTimeOffset.UtcNow.AddDays(1));

    [Fact]
    public async Task Saved_code_can_be_redeemed_once()
    {
        var store = CreateStore();
        await store.SaveAsync(Entry(7, "ABCD2345"), "ABCD2345", Day);

        var redeemed = await store.RedeemAsync(" abcd 2345 ");

        Assert.NotNull(redeemed);
        Assert.Equal(7, redeemed!.OrganizationId);
        Assert.Equal(OrganizationRole.Editor, redeemed.Role);

        // Код одноразовый: повторная попытка ничего не находит.
        Assert.Null(await store.RedeemAsync("ABCD2345"));
    }

    [Fact]
    public async Task Code_lives_for_a_day()
    {
        var store = CreateStore();
        await store.SaveAsync(Entry(7, "ABCD2345"), "ABCD2345", Day);

        var server = _redis!.GetServers().First();
        var keys = server.Keys(pattern: "invite:*").ToArray();

        Assert.NotEmpty(keys);

        // Оба ключа (код и ссылка на него из организации) живут одинаково — сутки.
        foreach (var key in keys)
        {
            var ttl = await _redis.GetDatabase().KeyTimeToLiveAsync(key);

            Assert.NotNull(ttl);
            Assert.InRange(ttl!.Value, Day - TimeSpan.FromMinutes(1), Day);
        }
    }

    [Fact]
    public async Task New_code_replaces_previous_one()
    {
        var store = CreateStore();
        await store.SaveAsync(Entry(7, "ABCD2345"), "ABCD2345", Day);
        await store.SaveAsync(Entry(7, "QWER6789"), "QWER6789", Day);

        Assert.Null(await store.RedeemAsync("ABCD2345"));

        var redeemed = await store.RedeemAsync("QWER6789");
        Assert.NotNull(redeemed);
        Assert.Equal(7, redeemed!.OrganizationId);
    }

    [Fact]
    public async Task Codes_of_different_organizations_coexist()
    {
        var store = CreateStore();
        await store.SaveAsync(Entry(7, "ABCD2345"), "ABCD2345", Day);
        await store.SaveAsync(Entry(8, "QWER6789"), "QWER6789", Day);

        Assert.Equal(7, (await store.RedeemAsync("ABCD2345"))!.OrganizationId);
        Assert.Equal(8, (await store.RedeemAsync("QWER6789"))!.OrganizationId);
    }

    [Fact]
    public async Task Expired_code_is_rejected()
    {
        // Часы в будущем: код ещё лежит в Redis, но его срок уже прошёл.
        var store = CreateStore(new TestTimeProvider(DateTimeOffset.UtcNow.AddDays(2)));
        await store.SaveAsync(Entry(7, "ABCD2345"), "ABCD2345", Day);

        Assert.Null(await store.RedeemAsync("ABCD2345"));
    }

    [Fact]
    public async Task Unknown_code_is_rejected()
    {
        Assert.Null(await CreateStore().RedeemAsync("ZZZZ9999"));
    }
}
