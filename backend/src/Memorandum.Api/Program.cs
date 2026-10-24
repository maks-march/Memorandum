using Memorandum.Api;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ── Конфигурация ─────────────────────────────────────────────────────────────
var env = builder.Environment;
var provider = builder.Configuration["Database:Provider"] ?? "Sqlite";

// Fail-fast: в проде не бывает SQLite и непустых секретов в appsettings
if (env.IsProduction())
{
    if (!string.Equals(provider, "Postgres", StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException(
            "Production требует Database:Provider = Postgres. Проверьте appsettings.Production.json.");

    if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("Postgres")))
        throw new InvalidOperationException(
            "Production требует ConnectionStrings:Postgres (переменная окружения ConnectionStrings__Postgres).");
}

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// dev: фронт может ходить напрямую (VITE_API_MODE=remote), иначе всё идёт через Vite-прокси
builder.Services.AddCors(o => o.AddPolicy("dev", p => p
    .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

var app = builder.Build();

app.UseExceptionHandler("/api/error");

// OpenAPI/Scalar: в Development включены всегда, в Production — только флагом OpenApi:Enabled=true
var openApiEnabled = app.Configuration.GetValue("OpenApi:Enabled", app.Environment.IsDevelopment());
if (openApiEnabled)
{
    app.MapOpenApi();                                        // /openapi/v1.json
    app.MapScalarApiReference(o => o.WithTitle("Memorandum API"));   // /scalar/v1
}

if (app.Environment.IsDevelopment())
{
    app.UseCors("dev");                                      // фронт может ходить напрямую, без Vite-прокси
}

app.MapControllers();

app.Run();
