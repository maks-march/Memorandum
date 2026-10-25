using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Memorandum.Api.Controllers;

/// <summary>Проверка живости сервиса.</summary>
[ApiController]
[ApiVersionNeutral]
[Route("api/health")]
[Tags("Health")]
public sealed class HealthController(IConfiguration config, IHostEnvironment env) : ControllerBase
{
    /// <summary>Диагностический health: окружение, провайдер БД, версия сборки (в ней — git sha).</summary>
    /// <response code="200">Сервис работает.</response>
    [HttpGet]
    [ProducesResponseType<HealthResponse>(StatusCodes.Status200OK)]
    public ActionResult<HealthResponse> Get() =>
        Ok(new HealthResponse(
            Status: "ok",
            Mode: "api",
            Environment: env.EnvironmentName,
            Database: config["Database:Provider"] ?? "Sqlite",
            Version: BuildInfo.Version,
            Machine: Environment.MachineName,
            TimeUtc: DateTime.UtcNow));
}

/// <param name="Status">Всегда "ok" — это ответ живого API.</param>
/// <param name="Mode">"api" — реальный бэкенд, "mock" — заглушка.</param>
/// <param name="Environment">Development / Production.</param>
/// <param name="Database">Sqlite (локально) или Postgres (прод).</param>
/// <param name="Version">Версия сборки API: семантическая версия + короткий git sha (<c>1.0.0+abc1234</c>).</param>
/// <param name="Machine">Имя машины/контейнера — видно, что ответил именно api, а не мок.</param>
/// <param name="TimeUtc">Момент ответа, UTC.</param>
public sealed record HealthResponse(
    string Status,
    string Mode,
    string Environment,
    string Database,
    string Version,
    string Machine,
    DateTime TimeUtc);
