using StackExchange.Redis;

namespace Memorandum.Tests.Infrastructure;

/// <summary>
/// Подключение к Redis из тестов. Одно место, где решается, что считать
/// «Redis недоступен» и как об этом сообщить: адрес берётся из <c>REDIS</c>,
/// по умолчанию — локальный экземпляр.
/// <para>
/// Тесты Redis идут последовательно (см. <c>xunit.runner.json</c>): они пишут
/// в общий экземпляр и Redis-тесты хранилища делают <c>FLUSHDB</c>. При
/// параллельном запуске сценарий смены пароля терял бы свой тикет между
/// запросами — отсюда и «падает, а на перезапуске проходит».
/// </para>
/// </summary>
internal static class RedisTestServer
{
    /// <summary>Строка подключения для приложения под тестом: падать при потере связи оно не должно.</summary>
    public static string Configuration => Address + ",abortConnect=false,connectTimeout=2000";

    /// <summary>Адрес без надстроек — для сценариев и проверок.</summary>
    public static string Address => Environment.GetEnvironmentVariable("REDIS") ?? "localhost:6379";

    /// <summary>
    /// Соединение для проверок. <c>AbortOnConnectFail</c> включён намеренно: иначе
    /// клиент молча ждёт в бэклоге и тест падает по таймауту вместо понятной причины.
    /// </summary>
    public static async Task<IConnectionMultiplexer> ConnectAsync()
    {
        var options = ConfigurationOptions.Parse(Address);
        options.AbortOnConnectFail = true;
        options.ConnectTimeout = 2000;
        options.SyncTimeout = 2000;

        try
        {
            await using var probe = await ConnectionMultiplexer.ConnectAsync(options);
            await probe.GetDatabase().PingAsync();
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                "Redis недоступен (" + Address + "): тестам нужен живой Redis. " +
                "Поднять рядом: `docker compose -f infra/docker-compose.dev.yml up -d redis` " +
                "или `docker run -d -p 6379:6379 redis:7-alpine`. " + exception.Message,
                exception);
        }

        return await ConnectionMultiplexer.ConnectAsync(options);
    }

    /// <summary>Кулдаун переживает прогоны: перед сценарием его нужно снять.</summary>
    public static async Task ClearCooldownAsync(long userId)
    {
        await using var redis = await ConnectAsync();
        await redis.GetDatabase().KeyDeleteAsync($"pwd:cooldown:{userId}");
    }
}
