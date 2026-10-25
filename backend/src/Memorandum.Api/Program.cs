using System.Diagnostics;
using Asp.Versioning;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Memorandum.Api;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var env = builder.Environment;
var provider = builder.Configuration["Database:Provider"] ?? "Sqlite";
var allowedHosts = builder.Configuration["AllowedHosts"];

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
builder.Services.AddProblemDetails();

builder.Services
    .AddApiVersioning(o =>
    {
        o.DefaultApiVersion = new ApiVersion(1, 0);
        o.AssumeDefaultVersionWhenUnspecified = true;
        o.ReportApiVersions = true;
        o.ApiVersionReader = new HeaderApiVersionReader("X-Api-Version");
    })
    .AddMvc()
    .AddApiExplorer(o =>
    {
        o.GroupNameFormat = "'v'VVV";
        o.SubstituteApiVersionInUrl = false;
    })
    .AddOpenApi();

builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

builder.Services.AddCors(o => o.AddPolicy("dev", p => p
    .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

var app = builder.Build();

app.UseForwardedHeaders();

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

    logger.LogError(error, "Необработанное исключение. traceId={TraceId} path={Path}", traceId, context.Request.Path);

    var problem = new ProblemDetails
    {
        Status = StatusCodes.Status500InternalServerError,
        Title = "Внутренняя ошибка сервера",
        Detail = app.Environment.IsDevelopment() ? error?.ToString() : "Обратитесь к администратору и сообщите traceId.",
        Instance = context.Request.Path
    };
    problem.Extensions["traceId"] = traceId;

    await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
}));

app.UseStatusCodePages();

var openApiEnabled = app.Configuration.GetValue("OpenApi:Enabled", app.Environment.IsDevelopment());
if (openApiEnabled)
{
    if (env.IsProduction())
        app.Logger.LogWarning("OpenApi/Scalar включены в Production (OpenApi:Enabled=true). Не оставляйте так постоянно.");

    app.MapOpenApi().WithDocumentPerVersion();
    app.MapScalarApiReference(o => o
        .WithTitle("Memorandum API")
        .AddDocument("v1", "API v1", "/openapi/v1.json", isDefault: true));
}

if (env.IsDevelopment())
{
    app.UseCors("dev");
}

app.MapControllers();

app.Run();

/// <summary>Точка входа. Публичный partial нужен интеграционным тестам (WebApplicationFactory&lt;Program&gt;).</summary>
public partial class Program;
