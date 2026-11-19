using Microsoft.EntityFrameworkCore;
using Memorandum.Infrastructure.Persistence;

namespace Memorandum.Api.Infrastructure;

/// <summary>
/// Подготовка схемы на старте. Миграции существуют только для Postgres (§4.3 плана);
/// для dev-SQLite схема создаётся из модели, иначе EF сравнивал бы модель SQLite
/// со снимком, сгенерированным для Npgsql, и падал с pending model changes.
/// Пока реплика одна — допустимо; при масштабировании переносится в шаг деплоя.
/// </summary>
internal static class AppExtensions
{
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (db.Database.IsNpgsql())
        {
            var pending = (await db.Database.GetPendingMigrationsAsync()).ToArray();

            if (pending.Length == 0)
            {
                app.Logger.LogInformation("Миграции: схема актуальна");
                return;
            }

            app.Logger.LogInformation(
                "Миграции: применяю {Count} — {Names}", pending.Length, string.Join(", ", pending));

            await db.Database.MigrateAsync();
            app.Logger.LogInformation("Миграции: применены");
            return;
        }

        var created = await db.Database.EnsureCreatedAsync();
        app.Logger.LogInformation("Схема dev-базы: {State}", created ? "создана из модели" : "актуальна");
    }
}
