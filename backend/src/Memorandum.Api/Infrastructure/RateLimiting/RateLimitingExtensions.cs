using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Memorandum.Domain.Common;

namespace Memorandum.Api.Infrastructure.RateLimiting;

/// <summary>
/// Встроенный rate limiter: фиксированное окно по IP. Защищает вход и регистрацию
/// от перебора пароля. Счётчики живут в памяти процесса, поэтому на нескольких
/// репликах лимит действует на каждую отдельно (для общего счётчика нужен Redis).
/// </summary>
internal static class RateLimitingExtensions
{
    /// <summary>Политика для небезопасных auth-эндпоинтов (регистрация, вход).</summary>
    public const string AuthPolicy = "auth";

    /// <summary>Общая политика для остального API.</summary>
    public const string GlobalPolicy = "global";

    public static IServiceCollection AddApiRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetSection(RateLimitOptions.SectionName).Get<RateLimitOptions>()
                      ?? new RateLimitOptions();

        services.Configure<RateLimitOptions>(configuration.GetSection(RateLimitOptions.SectionName));

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiter.AddPolicy(AuthPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                PartitionKey(context, "auth"),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.AuthPermitLimit,
                    Window = TimeSpan.FromSeconds(options.AuthWindowSeconds),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                }));

            limiter.AddPolicy(GlobalPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                PartitionKey(context, "global"),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.GlobalPermitLimit,
                    Window = TimeSpan.FromSeconds(options.GlobalWindowSeconds),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                }));

            limiter.OnRejected = WriteProblemAsync;
        });

        return services;
    }

    /// <summary>Ключ раздела: реальный IP клиента (уже восстановленный из X-Forwarded-For).</summary>
    private static string PartitionKey(HttpContext context, string policy)
    {
        var address = context.Connection.RemoteIpAddress ?? IPAddress.None;

        // IPv4 в IPv6-форме (::ffff:10.0.0.1) — это тот же клиент: приводим к одной форме.
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();

        return $"{policy}:{address}";
    }

    /// <summary>429 в формате ProblemDetails с Retry-After: клиент знает, когда повторять.</summary>
    private static async ValueTask WriteProblemAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;

        if (httpContext.Response.HasStarted)
            return;

        var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var metadata)
            ? metadata
            : TimeSpan.FromSeconds(60);

        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        httpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Слишком много запросов",
            Detail = $"Повторите запрос через {(int)Math.Ceiling(retryAfter.TotalSeconds)} с.",
            Instance = httpContext.Request.Path,
        };
        problem.Extensions["code"] = ErrorCodes.TooManyRequests;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        await httpContext.Response.WriteAsJsonAsync(
            problem,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web),
            "application/problem+json",
            cancellationToken);
    }
}
