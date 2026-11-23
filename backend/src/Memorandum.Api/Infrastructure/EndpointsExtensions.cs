using Microsoft.EntityFrameworkCore;
using Memorandum.Infrastructure.Persistence;
using Scalar.AspNetCore;

namespace Memorandum.Api.Infrastructure;

/// <summary>
/// Карта приложения: контроллеры, справочник API и подготовка схемы БД.
/// </summary>
internal static class EndpointsExtensions
{
    public static WebApplication UseApiDocumentation(this WebApplication app)
    {
        var enabled = app.Configuration.GetValue("OpenApi:Enabled", app.Environment.IsDevelopment());

        if (!enabled)
            return app;

        if (app.Environment.IsProduction())
            app.Logger.LogWarning("OpenApi/Scalar включены в Production (OpenApi:Enabled=true). Не оставляйте так постоянно.");

        app.MapOpenApi().WithDocumentPerVersion();
        app.MapScalarApiReference(o => o
            .WithTitle("Memorandum API")
            .AddDocument("v1", "API v1", "/openapi/v1.json", isDefault: true));

        return app;
    }

    /// <summary>
    /// Схема на старте: применяются миграции. Провайдер один (Postgres), поэтому
    /// миграции подходят и dev-стенду, и проду. Пока реплика одна — допустимо;
    /// при масштабировании переносится в шаг деплоя.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        // Схема нужна приложению, но не всегда нужна проверяющему его стенду:
        // в CI образ поднимается без Postgres (Database__ApplyMigrationsOnStartup=false).
        if (!app.Configuration.GetValue("Database:ApplyMigrationsOnStartup", true))
        {
            app.Logger.LogWarning("Миграции на старте отключены (Database:ApplyMigrationsOnStartup=false)");
            return;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

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
    }
}
