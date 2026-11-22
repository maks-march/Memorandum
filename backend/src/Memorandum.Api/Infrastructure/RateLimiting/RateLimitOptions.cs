namespace Memorandum.Api.Infrastructure.RateLimiting;

/// <summary>Настройки ограничения частоты запросов (<c>RateLimit:*</c> в конфигурации).</summary>
public sealed class RateLimitOptions
{
    public const string SectionName = "RateLimit";

    /// <summary>Сколько запросов к auth-эндпоинтам допускается за окно с одного IP.</summary>
    public int AuthPermitLimit { get; init; } = 10;

    /// <summary>Длина окна в секундах.</summary>
    public int AuthWindowSeconds { get; init; } = 60;

    /// <summary>Сколько запросов ко всему API допускается за окно с одного IP. 0 — без общего лимита.</summary>
    public int GlobalPermitLimit { get; init; } = 300;

    public int GlobalWindowSeconds { get; init; } = 60;
}
