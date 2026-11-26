using Asp.Versioning;
using Microsoft.AspNetCore.Antiforgery;
using Memorandum.Api.Auth;
using Memorandum.Api.Infrastructure.Errors;
using Memorandum.Api.Infrastructure.RateLimiting;

namespace Memorandum.Api.Infrastructure;

/// <summary>
/// Регистрация сервисов API по категориям: безопасность, версионирование, CORS.
/// Конфигурация конвейера — в <see cref="ApplicationPipelineExtensions"/>.
/// </summary>
internal static class ServiceCollectionExtensions
{
    /// <summary>
    /// Антифоржери в паре с cookie-аутентификацией, ограничение частоты и часы.
    /// </summary>
    public static IServiceCollection AddApiSecurity(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        // AddControllersWithViews нужен ради штатного фильтра [RequireAntiforgeryToken]:
        // с одним AddControllers() провал проверки не превращается в 400.
        services.AddControllersWithViews(options => options.Filters.Add<ApiClientErrorResultFilter>())
            .ConfigureApplicationPartManager(parts =>
            {
                // Заглушки /api/v1/mocks/... — часть приложения, но подключаются только
                // при Mock:Enabled: в проде контроллеров моков в контейнере нет.
                if (!configuration.GetValue("Mock:Enabled", false))
                    parts.FeatureProviders.Add(new MocksExcludedProvider());
            });
        services.AddProblemDetails();
        services.AddAntiforgery(options => ConfigureAntiforgery(options, environment));

        services.AddApiCookieAuthentication(environment);
        services.AddApiRateLimiting(configuration);

        // Единый источник текущего времени: сценарии берут GetUtcNow() один раз на операцию,
        // тесты подменяют его детерминированной реализацией.
        services.AddSingleton(TimeProvider.System);

        return services;
    }

    /// <summary>Версии в сегменте URL: <c>/api/v1/...</c>; OpenAPI — по одной схеме на версию.</summary>
    public static IServiceCollection AddApiVersioningAndDocs(this IServiceCollection services)
    {
        services
            .AddApiVersioning(o =>
            {
                o.DefaultApiVersion = new ApiVersion(1, 0);
                o.ReportApiVersions = true;
                o.ApiVersionReader = new UrlSegmentApiVersionReader();
            })
            .AddMvc()
            .AddApiExplorer(o =>
            {
                o.GroupNameFormat = "'v'VVV";
                o.SubstituteApiVersionInUrl = true;
            })
            .AddOpenApi();

        return services;
    }

    /// <summary>
    /// CORS нужен только dev-стенду (фронт на :5173 и API на :5000 — разные origin).
    /// В проде оба за nginx на одном origin, поэтому политика не включается.
    /// </summary>
    public static IServiceCollection AddApiCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration["Cors:AllowedOrigins"]?
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

        return services.AddCors(o => o.AddPolicy(DevCorsPolicy, p => p
            .WithOrigins(allowedOrigins.Length > 0
                ? allowedOrigins
                : ["http://localhost:5173", "http://127.0.0.1:5173"])
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));
    }

    public const string DevCorsPolicy = "dev";

    /// <summary>Cookie с антифоржери-токеном: браузер отправляет её вместе с заголовком.</summary>
    public const string AntiforgeryCookieName = "XSRF-TOKEN";

    /// <summary>Заголовок, в который клиент кладёт токен из <c>GET /api/v1/auth/csrf</c>.</summary>
    public const string AntiforgeryHeaderName = "X-XSRF-TOKEN";

    /// <summary>Двойная отправка: cookie и тот же токен в заголовке.</summary>
    /// <remarks>
    /// Заголовок <c>X-XSRF-TOKEN</c> ставит клиент, сервер его не подставляет: иначе
    /// сторонний сайт мог бы рассчитывать на cookie браузера, и защита сводилась бы
    /// к <c>SameSite</c>. <c>HttpOnly</c> выключен — токен читает фронт.
    /// </remarks>
    private static void ConfigureAntiforgery(AntiforgeryOptions options, IWebHostEnvironment environment)
    {
        options.HeaderName = AntiforgeryHeaderName;
        options.FormFieldName = "__RequestVerificationToken";
        options.Cookie.Name = AntiforgeryCookieName;
        options.Cookie.HttpOnly = false;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest          // dev идёт по http
            : CookieSecurePolicy.Always;
    }
}
