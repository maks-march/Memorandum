using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Memorandum.Infrastructure.Persistence;
using Memorandum.Tests.Infrastructure;

namespace Memorandum.Tests;

/// <summary>
/// Поднимает реальное приложение в памяти (без сети и без docker).
/// WebApplicationFactory проходит весь конвейер middleware: routing, версионирование,
/// фильтры, обработчик исключений — поэтому тесты проверяют настоящее поведение API.
/// </summary>
public class MemorandumApiFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// Своя база на каждый стенд: так тесты не делят состояние, а миграции на старте
    /// проверяют ту же схему, что и прод. Удаляется вместе с фабрикой.
    /// </summary>
    private PostgresTestDatabase? _ownDatabase;

    /// <summary>Часы, которые получат сценарии и контроллеры. <c>null</c> — системные.</summary>
    public TimeProvider? TimeProvider { get; init; }

    /// <summary>
    /// Строка подключения к тестовой базе. Если задана, регистрируется вместо
    /// строки из конфигурации: каждый тест работает на своей базе.
    /// </summary>
    public string? ConnectionString { get; private set; }

    /// <summary>Контекст на базе стенда: проверка того, что реально сохранилось.</summary>
    public AppDbContext NewContext() =>
        PostgresTestDatabase.NewContext(ConnectionString!);

    /// <summary>Окружение, в котором стартует приложение.</summary>
    public string Environment { get; init; } = "Development";

    /// <summary>Последний шаг настройки контейнера: тест подменяет сервисы после регистрации приложения.</summary>
    public Action<IServiceCollection>? ConfigureServices { get; init; }

    /// <summary>Переопределения конфигурации (аналог appsettings/переменных окружения).</summary>
    public Dictionary<string, string?> Settings { get; init; } = new()
    {
        ["Database:Provider"] = "Postgres",
        ["Mock:Enabled"] = "true",
        ["PasswordReset:CodePepper"] = "test-pepper",
        ["Invitations:CodePepper"] = "test-pepper",
        ["Redis:Configuration"] = "localhost:6379,abortConnect=false,connectTimeout=1000",
        // Тесты не должны упираться в лимит: конкретные значения задаются в RateLimitTests.
        ["RateLimit:AuthPermitLimit"] = "1000",
        ["RateLimit:GlobalPermitLimit"] = "1000",
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environment);

        foreach (var (key, value) in Settings)
            builder.UseSetting(key, value);

        // База создаётся один раз на фабрику: схему накатывает приложение на старте.
        ConnectionString ??= (_ownDatabase ??= PostgresTestDatabase.CreateAsync().GetAwaiter().GetResult())
            .ConnectionString;

        builder.ConfigureTestServices(services =>
        {
            services.AddControllers().AddApplicationPart(typeof(TestOnlyBoomController).Assembly);
            ConfigureServices?.Invoke(services);

            // Последняя регистрация побеждает: часы из теста заменяют TimeProvider.System.
            if (TimeProvider is not null)
                services.AddSingleton(TimeProvider);

            services.RemoveAll<DbContextOptions>();
            services.AddDbContext<AppDbContext>(options => options.UseNpgsql(ConnectionString!));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        // Базу, созданную фабрикой, убираем вместе с ней; чужую (из фикстуры) — нет.
        _ownDatabase?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        _ownDatabase = null;
    }
}
