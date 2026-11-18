using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Memorandum.Infrastructure.Persistence;

namespace Memorandum.Tests.Infrastructure;

/// <summary>
/// База в памяти на время теста. Схема создаётся из модели, поэтому проверяются
/// и индексы, и внешние ключи, в отличие от in-memory провайдера EF.
/// </summary>
public sealed class SqliteDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private SqliteDatabase(SqliteConnection connection, AppDbContext context)
    {
        _connection = connection;
        Context = context;
    }

    public AppDbContext Context { get; }

    public static async Task<SqliteDatabase> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var context = NewContext(connection);
        await context.Database.EnsureCreatedAsync();

        return new SqliteDatabase(connection, context);
    }

    /// <summary>Новый контекст на той же базе: проверка, что данные действительно сохранены.</summary>
    public AppDbContext NewContext() => NewContext(_connection);

    public static AppDbContext NewContext(SqliteConnection connection) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
