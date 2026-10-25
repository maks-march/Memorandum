using System.Reflection;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Memorandum.Api.Controllers;

/// <summary>
/// Сведения о версии API. Версия выбирается сегментом URL: <c>/api/v1/system/info</c>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/system")]
[Tags("System")]
public sealed class SystemController(IHostEnvironment env) : ControllerBase
{
    /// <summary>Версии API, поддерживаемые сервисом, и версия сборки.</summary>
    /// <response code="200">Информация о версии API.</response>
    [HttpGet("info")]
    [ProducesResponseType<SystemInfoResponse>(StatusCodes.Status200OK)]
    public ActionResult<SystemInfoResponse> Info() =>
        Ok(new SystemInfoResponse(
            ApiVersions: ["1.0"],
            AppVersion: Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0",
            Environment: env.EnvironmentName,
            RequestedVersion: Format(HttpContext.RequestedApiVersion)));

    private static string? Format(ApiVersion? version) =>
        version is null ? null : $"{version.MajorVersion}.{version.MinorVersion ?? 0}";
}

/// <param name="ApiVersions">Поддерживаемые версии API.</param>
/// <param name="AppVersion">Версия сборки сервиса.</param>
/// <param name="Environment">Окружение (Development/Production).</param>
/// <param name="RequestedVersion">Версия, определённая для текущего запроса (из сегмента URL).</param>
public sealed record SystemInfoResponse(
    string[] ApiVersions,
    string AppVersion,
    string Environment,
    string? RequestedVersion);
