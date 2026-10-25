using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Memorandum.Api.Controllers;

/// <summary>
/// Заглушка для фронтенда: тот же контракт, что /api/health, но с управляемыми
/// задержкой и кодом ответа. Включается флагом Mock:Enabled (по умолчанию только в Development).
/// </summary>
[ApiController]
[ApiVersionNeutral]
[Route("mock/api/health")]
[Tags("Health (mock)")]
public sealed class MockHealthController(IConfiguration config, IHostEnvironment env) : ControllerBase
{
    private bool Enabled => config.GetValue("Mock:Enabled", env.IsDevelopment());

    /// <summary>Фейковый health с имитацией задержки и ошибок.</summary>
    /// <param name="delay">Задержка ответа в мс (0–10000).</param>
    /// <param name="status">HTTP-код, который нужно сымитировать: 200 (по умолчанию), 401, 404, 500.</param>
    /// <param name="ct">Токен отмены запроса.</param>
    /// <response code="200">Данные заглушки.</response>
    /// <response code="401">Сымитированная ошибка авторизации.</response>
    /// <response code="404">Заглушка выключена.</response>
    /// <response code="500">Сымитированная ошибка сервера.</response>
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int delay = 0,
        [FromQuery] int status = StatusCodes.Status200OK,
        CancellationToken ct = default)
    {
        if (!Enabled)
            return NotFound(new { error = "mock_disabled", message = "Заглушка выключена (Mock:Enabled=false)." });

        if (delay is > 0 and <= 10_000)
            await Task.Delay(delay, ct);

        if (status is 401 or 404 or 500)
            return StatusCode(status, new
            {
                error = status switch
                {
                    401 => "unauthorized",
                    404 => "not_found",
                    _ => "internal_error"
                },
                message = "Сымитированная ошибка из mock-эндпоинта.",
                mode = "mock"
            });

        return Ok(new
        {
            status = "ok",
            mode = "mock",
            environment = env.EnvironmentName,
            database = "none (mock)",
            version = "mock-1.0.0",
            machine = "mock",
            timeUtc = DateTime.UtcNow,
            requestId = Guid.NewGuid()
        });
    }
}
