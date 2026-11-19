using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Memorandum.Infrastructure.Persistence;

namespace Memorandum.Tests;

/// <summary>
/// Поднимает реальное приложение в памяти (без сети и без docker).
/// WebApplicationFactory проходит весь конвейер middleware: routing, версионирование,
/// фильтры, обработчик исключений — поэтому тесты проверяют настоящее поведение API.
/// </summary>
public class MemorandumApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Часы, которые получат сценарии и контроллеры. <c>null</c> — системные.</summary>
    public TimeProvider? TimeProvider { get; init; }

    /// <summary>
    /// Соединение с БД. Если задано, регистрируется вместо настроенного провайдера:
    /// тесты с HTTP работают на SQLite в памяти, а не на файле из appsettings.
    /// </summary>
    public SqliteConnection? Database { get; init; }

    /// <summary>Окружение, в котором стартует приложение.</summary>
    public string Environment { get; init; } = "Development";

    /// <summary>Переопределения конфигурации (аналог appsettings/переменных окружения).</summary>
    public Dictionary<string, string?> Settings { get; init; } = new()
    {
        ["Database:Provider"] = "Sqlite",
        ["Mock:Enabled"] = "true",
        // Тесты не должны упираться в лимит: конкретные значения задаются в RateLimitTests.
        ["RateLimit:AuthPermitLimit"] = "1000",
        ["RateLimit:GlobalPermitLimit"] = "1000",
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environment);

        foreach (var (key, value) in Settings)
            builder.UseSetting(key, value);

        builder.ConfigureTestServices(services =>
        {
            services.AddControllers().AddApplicationPart(typeof(TestOnlyBoomController).Assembly);

            // Последняя регистрация побеждает: часы из теста заменяют TimeProvider.System.
            if (TimeProvider is not null)
                services.AddSingleton(TimeProvider);

            if (Database is not null)
            {
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<DbContextOptions>();
                services.AddDbContext<AppDbContext>(options => options.UseSqlite(Database));
            }
        });
    }
}
