using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Memorandum.Infrastructure.Persistence;

namespace Memorandum.Tests.Infrastructure;

/// <summary>Соединение с базой в памяти и контексты на нём.</summary>
public sealed class SqliteConnectionHolder : IAsyncDisposable
{
    private SqliteConnectionHolder(SqliteConnection connection) => Connection = connection;

    public SqliteConnection Connection { get; }

    public static async Task<SqliteConnectionHolder> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        await using var context = NewContext(connection);
        await context.Database.EnsureCreatedAsync();

        return new SqliteConnectionHolder(connection);
    }

    public AppDbContext NewContext() => NewContext(Connection);

    public static AppDbContext NewContext(SqliteConnection connection) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options);

    public async ValueTask DisposeAsync() => await Connection.DisposeAsync();
}
