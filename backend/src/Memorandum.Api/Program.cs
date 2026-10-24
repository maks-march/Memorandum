using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Memorandum.Api;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ── Конфигурация ─────────────────────────────────────────────────────────────
var env = builder.Environment;
var provider = builder.Configuration["Database:Provider"] ?? "Sqlite";
var allowedHosts = builder.Configuration["AllowedHosts"];

// Fail-fast: прод не должен стартовать с dev-настройками
if (env.IsProduction())
{
    if (!string.Equals(provider, "Postgres", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException(
            "Production требует Database:Provider = Postgres. Проверьте appsettings.Production.json.");

    if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("Postgres")))
        throw new InvalidOperationException(
            "Production требует ConnectionStrings:Postgres (переменная окружения ConnectionStrings__Postgres).");

    if (builder.Configuration.GetValue("Mock:Enabled", false))
        throw new InvalidOperationException(
            "Production не может работать с Mock:Enabled=true. Заглушки должны быть отключены.");

    if (string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts.Contains('*'))
        throw new InvalidOperationException(
            "Production требует конкретный AllowedHosts (домен через ';'), значение '*' запрещено. " +
            "Задайте переменную окружения AllowedHosts.");
}

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// За nginx: реальный IP и схема запроса приходят в X-Forwarded-*.
// API доступен только внутри docker-сети, поэтому доверяем прокси из этой сети.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

// dev: фронт может ходить напрямую (без Vite-прокси)
builder.Services.AddCors(o => o.AddPolicy("dev", p => p
    .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

var app = builder.Build();

app.UseForwardedHeaders();

// Единый ответ на необработанное исключение: 500 + ProblemDetails + traceId.
// Детали (стек) отдаются только в Development.
app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

    logger.LogError(error, "Необработанное исключение. traceId={TraceId} path={Path}", traceId, context.Request.Path);

    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    context.Response.ContentType = "application/problem+json";

    var problem = new ProblemDetails
    {
        Status = StatusCodes.Status500InternalServerError,
        Title = "Внутренняя ошибка сервера",
        Detail = app.Environment.IsDevelopment() ? error?.ToString() : "Обратитесь к администратору и сообщите traceId.",
        Instance = context.Request.Path
    };
    problem.Extensions["traceId"] = traceId;

    await context.Response.WriteAsJsonAsync(problem);
}));

// OpenAPI/Scalar: в Development включены всегда, в Production — только флагом OpenApi:Enabled=true.
// В проде включение разрешено (нужно для демо), но фиксируется предупреждением в логе.
var openApiEnabled = app.Configuration.GetValue("OpenApi:Enabled", app.Environment.IsDevelopment());
if (openApiEnabled)
{
    if (env.IsProduction())
        app.Logger.LogWarning("OpenApi/Scalar включены в Production (OpenApi:Enabled=true). Не оставляйте так постоянно.");

    app.MapOpenApi();                                                    // /openapi/v1.json
    app.MapScalarApiReference(o => o.WithTitle("Memorandum API"));       // /scalar/v1
}

if (env.IsDevelopment())
{
    app.UseCors("dev");
}

app.MapControllers();

app.Run();
