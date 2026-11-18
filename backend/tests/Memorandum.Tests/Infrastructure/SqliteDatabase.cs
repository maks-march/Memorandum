using Microsoft.Data.Sqlite;
using Memorandum.Infrastructure.Persistence;

namespace Memorandum.Tests.Infrastructure;

/// <summary>
/// База в памяти на время теста: схема создаётся из модели, поэтому проверяются и индексы,
/// и внешние ключи — в отличие от in-memory провайдера EF.
/// </summary>
public sealed class SqliteDatabase : IAsyncDisposable
{
    private SqliteConnectionHolder? _holder;

    private SqliteDatabase(SqliteConnectionHolder holder, AppDbContext context)
    {
        _holder = holder;
        Context = context;
    }

    public AppDbContext Context { get; }

    public static async Task<SqliteDatabase> CreateAsync()
    {
        var holder = await SqliteConnectionHolder.CreateAsync();
        return new SqliteDatabase(holder, holder.NewContext());
    }

    /// <summary>Новый контекст на той же базе: проверка, что данные действительно сохранены.</summary>
    public AppDbContext NewContext() => _holder!.NewContext();

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await _holder!.DisposeAsync();
        _holder = null;
    }
}
