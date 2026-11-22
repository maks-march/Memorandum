using Memorandum.Application.Abstractions;
using Memorandum.Application.PasswordChange.Models;
using StackExchange.Redis;

namespace Memorandum.Tests.Infrastructure;

/// <summary>
/// Хранилище поверх «мёртвого» адреса: воспроизводит упавший Redis
/// без остановки настоящего сервера.
/// </summary>
internal sealed class UnavailableRedisTicketStore : IPasswordResetTicketStore, IAsyncDisposable
{
    /// <summary>Порт, на котором заведомо ничего не слушает.</summary>
    public const string UnreachableServer = "127.0.0.1:6399,abortConnect=false,connectTimeout=300,syncTimeout=300";

    private readonly IConnectionMultiplexer _redis = ConnectionMultiplexer.Connect(
        ConfigurationOptions.Parse(UnreachableServer).With(o => o.AbortOnConnectFail = false));

    public Task<bool> TryStartCooldownAsync(long userId, TimeSpan cooldown, CancellationToken cancellationToken = default) =>
        _redis.GetDatabase().StringSetAsync($"pwd:cooldown:{userId}", "1", cooldown, When.NotExists);

    public Task SaveAsync(long userId, PasswordResetTicket ticket, TimeSpan timeToLive, CancellationToken cancellationToken = default) =>
        _redis.GetDatabase().StringSetAsync($"pwd:reset:{userId}", "{}", timeToLive);

    public Task<ConsumeCodeResult> ConsumeCodeAsync(long userId, string codeHash, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ConsumeCodeResult(ConsumeCodeStatus.NotFound, 0));

    public async ValueTask DisposeAsync() => await _redis.DisposeAsync();
}

file static class ConfigurationOptionsExtensions
{
    public static ConfigurationOptions With(this ConfigurationOptions options, Action<ConfigurationOptions> configure)
    {
        configure(options);
        return options;
    }
}
