using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Memorandum.Infrastructure.Persistence;

namespace Memorandum.Tests.Infrastructure;

/// <summary>
/// Приложение целиком на SQLite в памяти: HTTP-тесты не требуют Docker,
/// но работают на настоящем реляционном провайдере (индексы, FK, транзакции).
/// </summary>
public sealed class ApiFixture : IAsyncLifetime
{
    private SqliteConnectionHolder? _database;

    public MemorandumApiFactory Factory { get; private set; } = null!;

    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _database = await SqliteConnectionHolder.CreateAsync();

        Factory = new MemorandumApiFactory { Database = _database.Connection };
        Client = Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
            AllowAutoRedirect = false,
        });
    }

    public async Task DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();

        if (_database is not null)
            await _database.DisposeAsync();
    }

    internal AppDbContext NewContext() => _database!.NewContext();

    internal SqliteConnectionHolder Database => _database!;
}
