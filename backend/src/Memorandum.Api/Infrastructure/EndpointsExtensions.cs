using Microsoft.AspNetCore.Antiforgery;
using Microsoft.EntityFrameworkCore;
using Memorandum.Infrastructure.Persistence;
using Scalar.AspNetCore;

namespace Memorandum.Api.Infrastructure;

/// <summary>
/// Карта приложения: контроллеры, справочник API и подготовка схемы БД.
/// </summary>
internal static class EndpointsExtensions
{
    /// <summary>Имя схемы безопасности в Scalar: одна на весь API.</summary>
    private const string AntiforgerySchemeName = "Xsrf";

    private const string ScalarCsrfTokenKey = "ScalarCsrfToken";

    /// <summary>Скрипт страницы Scalar: держит токен актуальным.</summary>
    /// <remarks>
    /// Значение в конфиге Scalar статично, а вход и выход перевыпускают XSRF-токен
    /// вместе с cookie сессии. Поэтому скрипт хранит текущий токен сам, обновляет его
    /// через анонимный <c>GET /api/v1/auth/csrf</c> и подставляет заголовок в запросы
    /// мутаций — независимо от того, что показывает панель авторизации.
    /// </remarks>
    private static string ScalarAuthScript(string initialToken) =>
        $$"""
        <script>
        (() => {
          const header = "{{ServiceCollectionExtensions.AntiforgeryHeaderName}}";
          const safe = ["GET", "HEAD", "OPTIONS", "TRACE"];
          const refreshPaths = ["/api/v1/auth/login", "/api/v1/auth/logout"];
          let token = "{{initialToken}}";
          let refreshing = null;

          const refresh = () => {
            refreshing ??= fetch("/api/v1/auth/csrf", { credentials: "same-origin" })
              .then((response) => response.ok ? response.json() : null)
              .then((data) => {
                if (data?.xsrfToken) token = data.xsrfToken;
              })
              .catch(() => {})
              .finally(() => { refreshing = null; });

            return refreshing;
          };

          const absolute = (url) => {
            try {
              return new URL(url, window.location.href);
            } catch {
              return null;
            }
          };

          const withToken = (input, init) => {
            const headers = new Headers(init?.headers ?? input?.headers);
            headers.set(header, token);
            return headers;
          };

          const original = window.fetch.bind(window);

          window.fetch = (input, init) => {
            const url = absolute(typeof input === "string" ? input : input?.url ?? "");
            const method = (init?.method ?? (typeof input === "string" ? "GET" : input?.method ?? "GET")).toUpperCase();
            const sameOrigin = url?.origin === window.location.origin;

            const request = sameOrigin && !safe.includes(method)
              ? [input, { ...(init ?? {}), headers: withToken(input, init) }]
              : [input, init];

            return original(...request).then((response) => {
              // Сессия сменилась — следующий запрос должен уйти со свежим токеном.
              if (sameOrigin && refreshPaths.some((path) => url.pathname.endsWith(path)))
                response.clone().text().then(refresh, refresh);

              return response;
            });
          };

          // Токен мог истечь, пока страница открыта: обновляем перед первым запросом.
          document.addEventListener("DOMContentLoaded", () => { if (!token) refresh(); });
        })();
        </script>
        """;

    /// <summary>Токен для страницы Scalar.</summary>
    /// <remarks>
    /// <c>GetAndStoreTokens</c> на каждом вызове выдаёт новую пару «токен + cookie»,
    /// поэтому результат кэшируется на запрос: иначе в конфиг попадёт один токен,
    /// а в браузер уедет другой.
    /// </remarks>
    private static string ScalarCsrfToken(HttpContext httpContext)
    {
        if (httpContext.Items[ScalarCsrfTokenKey] is string cached)
            return cached;

        var token = httpContext.RequestServices
            .GetRequiredService<IAntiforgery>()
            .GetAndStoreTokens(httpContext)
            .RequestToken!;

        httpContext.Items[ScalarCsrfTokenKey] = token;
        return token;
    }

    public static WebApplication UseApiDocumentation(this WebApplication app)
    {
        var enabled = app.Configuration.GetValue("OpenApi:Enabled", app.Environment.IsDevelopment());

        if (!enabled)
            return app;

        if (app.Environment.IsProduction())
            app.Logger.LogWarning("OpenApi/Scalar включены в Production (OpenApi:Enabled=true). Не оставляйте так постоянно.");

        app.MapOpenApi().WithDocumentPerVersion();

        // Токен подставляется при выдаче страницы: вставлять заголовок руками не нужно.
        // Cookie XSRF-TOKEN ставится тем же вызовом, сессионную cookie браузер отправляет сам.
        app.MapScalarApiReference((options, httpContext) =>
        {
            options
                .WithTitle("Memorandum API")
                .AddPreferredSecuritySchemes(AntiforgerySchemeName)
                .AddApiKeyAuthentication(AntiforgerySchemeName, key => key
                    .WithName(ServiceCollectionExtensions.AntiforgeryHeaderName)
                    .WithValue(ScalarCsrfToken(httpContext)))
                .AddHeadContent(ScalarAuthScript(ScalarCsrfToken(httpContext)))
                .AddDocument("v1", "API v1", "/openapi/v1.json", isDefault: true);
        });

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
