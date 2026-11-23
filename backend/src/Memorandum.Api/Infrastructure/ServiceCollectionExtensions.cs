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
        services.AddControllersWithViews(options => options.Filters.Add<ApiClientErrorResultFilter>());
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

    /// <summary>Cookie с антифоржери-токеном: отправляется браузером сама, руками её не дублируем.</summary>
    public const string AntiforgeryCookieName = "XSRF-TOKEN";

    /// <summary>
    /// Антифоржери на cookie вместо заголовка.
    /// <para>
    /// ASP.NET умеет принимать токен из поля формы или заголовка, поэтому пара
    /// «cookie + заголовок» до сих пор была единственным способом. Сейчас токен
    /// едет только в cookie: клиенту не нужно читать его из ответа и подставлять
    /// в <c>X-XSRF-TOKEN</c>. Сравнение «двойная отправка» при этом сохраняется:
    /// cookie читается на том же origin, где выставляется, а сторонний сайт её
    /// отправить не может (SameSite=Lax + отсутствие CORS в Production).
    /// </para>
    /// <para>
    /// Компромисс: защита ослабевает до SameSite — при XSS токен тоже доступен.
    /// Поэтому <c>HttpOnly</c> остаётся выключенным (если токен читает фронт),
    /// а <c>Secure</c> и <c>SameSite=Lax</c> обязательны.
    /// </para>
    /// </summary>
    private static void ConfigureAntiforgery(AntiforgeryOptions options, IWebHostEnvironment environment)
    {
        // Имя заголовка штатное: его подставляет middleware AntiforgeryCookieMiddleware,
        // поэтому фронту не нужно знать про токен вообще.
        options.HeaderName = AntiforgeryCookieMiddleware.HeaderName;
        options.FormFieldName = "__RequestVerificationToken";
        options.Cookie.Name = AntiforgeryCookieName;
        options.Cookie.HttpOnly = false;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest          // dev идёт по http
            : CookieSecurePolicy.Always;
    }
}
