using Microsoft.AspNetCore.Mvc.Testing;
using Memorandum.Infrastructure.Persistence;

namespace Memorandum.Tests.Infrastructure;

/// <summary>
/// Приложение целиком на Postgres: HTTP-тесты видят ту же схему, индексы и ограничения,
/// что и прод. База создаётся миграциями и удаляется после прогона.
/// </summary>
public sealed class ApiFixture : IAsyncLifetime
{
    public MemorandumApiFactory Factory { get; private set; } = null!;

    public HttpClient Client { get; private set; } = null!;

    public Task InitializeAsync()
    {
        Factory = new MemorandumApiFactory();
        Client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
            AllowAutoRedirect = false,
        });

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
    }

    /// <summary>Контекст на базе стенда: проверка того, что реально сохранилось.</summary>
    internal AppDbContext NewContext() => Factory.NewContext();

    internal string ConnectionString => Factory.ConnectionString!;
}
