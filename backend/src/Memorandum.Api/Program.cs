using Memorandum.Api.Infrastructure;
using Memorandum.Api.Infrastructure.Errors;
using Memorandum.Application;
using Memorandum.Infrastructure;
using Scalar.AspNetCore;

// Точка входа читается сверху вниз как оглавление: проверки окружения, регистрация
// сервисов по категориям, затем конвейер и карта приложения. Детали — в Infrastructure.

var builder = WebApplication.CreateBuilder(args);

// 1. Окружение: в Production конфигурация проверяется до сборки контейнера.
builder.ProductionGuards();
builder.AddTrustedProxies();

// 2. Слои приложения.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// 3. API: безопасность (антифоржери, cookie-auth, лимиты, часы), версии, CORS.
builder.Services
    .AddApiSecurity(builder.Configuration, builder.Environment)
    .AddApiVersioningAndDocs();

if (builder.Environment.IsDevelopment())
    builder.Services.AddApiCors(builder.Configuration);

var app = builder.Build();

// 4. Конвейер: forward-заголовки → роутинг → лимиты → auth → антифоржери → ошибки.
app.UseApiPipeline();

// 5. Карта приложения: контроллеры, Scalar, схема БД.
app.UseApiDocumentation();

if (app.Environment.IsDevelopment())
    app.UseCors(ServiceCollectionExtensions.DevCorsPolicy);

app.MapControllers();

await app.ApplyMigrationsAsync();

app.Run();

/// <summary>Точка входа. Публичный partial нужен интеграционным тестам (WebApplicationFactory&lt;Program&gt;).</summary>
public partial class Program;
