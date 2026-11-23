using Microsoft.EntityFrameworkCore;
using Memorandum.Infrastructure.Persistence;
using Npgsql;

namespace Memorandum.Tests.Infrastructure;

/// <summary>
/// Отдельная база на время теста: создаётся пустой и удаляется после. Схему накатывает
/// приложение на старте миграциями — так тест заодно проверяет и сами миграции.
/// Один провайдер во всём проекте (Postgres), поэтому тесты проверяют ту же схему,
/// индексы и ограничения, что и прод.
/// <para>
/// Набор идёт последовательно вместе с остальными БД-тестами (см. <c>xunit.runner.json</c>):
/// базы создаются и удаляются на одном сервере.
/// </para>
/// </summary>
public sealed class PostgresTestDatabase : IAsyncDisposable
{
    private readonly string _adminConnectionString;

    private PostgresTestDatabase(string connectionString, string adminConnectionString, AppDbContext context)
    {
        ConnectionString = connectionString;
        _adminConnectionString = adminConnectionString;
        Context = context;
    }

    /// <summary>Строка подключения к базе этого теста — её получает приложение под тестом.</summary>
    public string ConnectionString { get; }

    public AppDbContext Context { get; }

    public static async Task<PostgresTestDatabase> CreateAsync()
    {
        var adminConnectionString = PostgresTestServer.AdminConnectionString;
        var name = $"memorandum_test_{Guid.NewGuid():N}";

        var builder = new NpgsqlConnectionStringBuilder(adminConnectionString) { Database = name };
        var connectionString = builder.ConnectionString;

        await using (var admin = new NpgsqlConnection(adminConnectionString))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        var context = NewContext(connectionString);

        // Схема — теми же миграциями, что и в проде: репозиторные тесты работают без
        // HTTP-стенда, а EnsureCreated разошёлся бы со снимком модели.
        await context.Database.MigrateAsync();

        return new PostgresTestDatabase(connectionString, adminConnectionString, context);
    }

    /// <summary>Новый контекст на той же базе: проверка, что данные действительно сохранены.</summary>
    public AppDbContext NewContext() => NewContext(ConnectionString);

    public static AppDbContext NewContext(string connectionString) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString).Options);

    public async ValueTask DisposeAsync()
    {
        var name = new NpgsqlConnectionStringBuilder(ConnectionString).Database!;

        await Context.DisposeAsync();
        NpgsqlConnection.ClearAllPools();

        await using var admin = new NpgsqlConnection(_adminConnectionString);
        await admin.OpenAsync();

        // WITH (FORCE) обрывает оставшиеся сессии: пул мог держать соединение после теста.
        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{name}\" WITH (FORCE)", admin);
        await drop.ExecuteNonQueryAsync();
    }
}
