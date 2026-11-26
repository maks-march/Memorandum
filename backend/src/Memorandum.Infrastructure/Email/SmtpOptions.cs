namespace Memorandum.Infrastructure.Email;

/// <summary>Настройки SMTP (<c>Smtp:*</c>).</summary>
/// <remarks>
/// Секрет — только пароль; адрес сервера и отправителя можно держать в конфигурации.
/// </remarks>
public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    /// <summary>STARTTLS на 587; неявный TLS (465) штатным <c>SmtpClient</c> не поддерживается.</summary>
    public bool UseStartTls { get; set; } = true;

    public string User { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    /// <summary>Адрес отправителя: «Memorandum &lt;no-reply@example.ru&gt;».</summary>
    public string From { get; set; } = string.Empty;

    /// <summary>Таймаут одной попытки отправки.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(15);
}
