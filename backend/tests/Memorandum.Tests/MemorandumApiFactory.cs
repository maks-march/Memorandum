using Microsoft.AspNetCore.HostFiltering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Memorandum.Tests;

/// <summary>
/// Поднимает реальное приложение в памяти (без сети и без docker).
/// WebApplicationFactory проходит весь конвейер middleware: routing, версионирование,
/// фильтры, обработчик исключений — поэтому тесты проверяют настоящее поведение API.
/// </summary>
public class MemorandumApiFactory : WebApplicationFactory<Program>
{
    /// <summary>Окружение, в котором стартует приложение.</summary>
    public string Environment { get; init; } = "Development";

    /// <summary>Переопределения конфигурации (аналог appsettings/переменных окружения).</summary>
    public Dictionary<string, string?> Settings { get; init; } = new()
    {
        ["Database:Provider"] = "Sqlite",
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environment);

        foreach (var (key, value) in Settings)
            builder.UseSetting(key, value);

                builder.ConfigureTestServices(services =>
        {
            services.AddControllers().AddApplicationPart(typeof(TestOnlyBoomController).Assembly);

            // HostFilteringMiddleware стоит до роутинга и отбивает Host, которого нет в
            // AllowedHosts, пустым 400. Тестовый клиент WebApplicationFactory всегда ходит
            // на http://localhost, поэтому разрешаем его здесь — на уровне пайплайна,
            // не трогая AllowedHosts из прод-конфига (гвард «* запрещён» остаётся честным).
            services.Configure<HostFilteringOptions>(o =>
            {
                if (!o.AllowedHosts.Contains("localhost"))
                    o.AllowedHosts.Add("localhost");
            });
        });
    }
}
