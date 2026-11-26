using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Memorandum.Application.Abstractions;

namespace Memorandum.Infrastructure.Email;

/// <summary>Запасной вывод письма в лог.</summary>
/// <remarks>
/// Один механизм на два случая: почта совсем без SMTP (<see cref="ConsoleEmailSender"/>)
/// и недоступный SMTP вне Production (<see cref="SmtpEmailSender"/>). В Production
/// запасного пути нет: письмо с кодом не должно оставаться в логах сервера.
/// </remarks>
public sealed class EmailFallback(IHostEnvironment environment)
{
    /// <summary>Разрешён ли вывод в лог вместо отправки.</summary>
    public bool Allowed => !environment.IsProduction();

    /// <summary>Печатает письмо с предупреждением, что оно не отправлено.</summary>
    public void Write(ILogger logger, string email, string subject, string body)
    {
        if (!Allowed)
            throw new InvalidOperationException(
                "Запасной вывод письма в лог работает только вне Production.");

        logger.LogWarning(
            "ПИСЬМО НЕ ОТПРАВЛЕНО, вывод в лог. Кому={Email}, тема={Subject}\n{Body}",
            email, subject, body);
    }
}
