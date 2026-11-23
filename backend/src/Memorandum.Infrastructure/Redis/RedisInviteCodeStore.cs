using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Memorandum.Application.Invitations;
using Memorandum.Domain.Organizations;

namespace Memorandum.Infrastructure.Redis;

/// <summary>
/// Коды приглашения в Redis. Ключ — HMAC от кода: открытый код в хранилище
/// не попадает, а по ключу нельзя понять, какой код в нём лежит.
/// <para>
/// У организации один действующий код: ключ <c>invite:org:{id}</c> помнит хеш
/// текущего кода, поэтому выдача нового вытесняет старый (иначе старые коды
/// продолжали бы работать до истечения суток).
/// </para>
/// <para>
/// Погашение — Lua-скрипт: чтение, проверка и удаление одной атомарной операцией,
/// иначе два одновременных запроса с одним кодом создали бы двух сотрудников.
/// </para>
/// </summary>
public sealed class RedisInviteCodeStore(
    IConnectionMultiplexer redis,
    TimeProvider timeProvider,
    IOptions<InvitationOptions> options) : IInviteCodeStore
{
    private const string CodeKeyPrefix = "invite:code:";
    private const string OrganizationKeyPrefix = "invite:org:";

    private const string RedeemScript = """
        local raw = redis.call('GET', KEYS[1])
        if not raw then return { 0, '' } end

        local entry = cjson.decode(raw)

        if tonumber(entry.expiresAt) <= tonumber(ARGV[1]) then
            redis.call('DEL', KEYS[1])
            redis.call('DEL', KEYS[2])
            return { 0, '' }
        end

        redis.call('DEL', KEYS[1])
        redis.call('DEL', KEYS[2])
        return { 1, raw }
        """;

    /// <summary>Вид кода в Redis. Срок числом: cjson в Lua не разбирает ISO-строки.</summary>
    private sealed record CodePayload(long OrganizationId, string Role, long ExpiresAt);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task SaveAsync(
        InviteCodeEntry entry,
        string code,
        TimeSpan timeToLive,
        CancellationToken cancellationToken = default)
    {
        var database = redis.GetDatabase();

        // Предыдущий код организации снимаем до записи нового: так у неё
        // в любой момент времени не больше одного действующего кода.
        var previous = await database.StringGetAsync(OrganizationKey(entry.OrganizationId));

        if (previous.HasValue)
            await database.KeyDeleteAsync(CodeKey((string)previous!), CommandFlags.FireAndForget);

        var payload = new CodePayload(
            entry.OrganizationId,
            entry.Role.ToString(),
            entry.ExpiresAt.ToUnixTimeSeconds());

        await database.StringSetAsync(
            CodeKey(Hash(code)),
            JsonSerializer.Serialize(payload, Json),
            timeToLive);

        await database.StringSetAsync(
            OrganizationKey(entry.OrganizationId),
            Hash(code),
            timeToLive);
    }

    public async Task<InviteCodeEntry?> RedeemAsync(string code, CancellationToken cancellationToken = default)
    {
        var hash = Hash(code);
        var now = timeProvider.GetUtcNow().ToUnixTimeSeconds();

        // Организация неизвестна до чтения записи, а ключ организации нужен скрипту
        // для удаления — поэтому сначала читаем, потом гасим в одной операции.
        var database = redis.GetDatabase();
        var raw = await database.StringGetAsync(CodeKey(hash));

        if (!raw.HasValue)
            return null;

        var payload = JsonSerializer.Deserialize<CodePayload>((string)raw!, Json);

        if (payload is null)
            return null;

        var redeemed = await database.ScriptEvaluateAsync(
            RedeemScript,
            [CodeKey(hash), OrganizationKey(payload.OrganizationId)],
            [now]);

        var result = (RedisResult[]?)redeemed;

        // Скрипт вернул 0 — кодом уже воспользовались между чтением и скриптом.
        if (result is null || (long)result[0] != 1)
            return null;

        return new InviteCodeEntry(
            payload.OrganizationId,
            hash,
            Enum.Parse<OrganizationRole>(payload.Role),
            DateTimeOffset.FromUnixTimeSeconds(payload.ExpiresAt));
    }

    /// <summary>Хеш считается с pepper: по дампу Redis код не подобрать.</summary>
    private string Hash(string code) =>
        InviteCode.Hash(options.Value.CodePepper, InviteCode.Normalize(code));

    private static string CodeKey(string hash) => CodeKeyPrefix + hash;

    private static string OrganizationKey(long organizationId) => OrganizationKeyPrefix + organizationId;
}
