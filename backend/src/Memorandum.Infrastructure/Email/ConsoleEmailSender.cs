using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Memorandum.Application.Abstractions;

namespace Memorandum.Infrastructure.Email;

/// <summary>Почта без SMTP: письмо печатается в лог.</summary>
/// <remarks>
/// Вне Production допустима как основной способ отправки, если SMTP не настроен,
/// и как запасной, когда сервер недоступен. В Production такой отправки нет:
/// код должен уходить адресату, а не в лог.
/// </remarks>
public sealed class ConsoleEmailSender(
    IHostEnvironment environment,
    EmailFallback fallback,
    ILogger<ConsoleEmailSender> logger) : IEmailSender
{
    public Task SendPasswordResetCodeAsync(
        string email,
        long userId,
        string code,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default)
    {
        if (environment.IsProduction())
            throw new InvalidOperationException(
                "Консольная почта работает только вне Production: нужен SMTP.");

        fallback.Write(logger, email, "Код для смены пароля",
            $"Код: {code}, userId {userId}, действует до {expiresAt:O}");

        return Task.CompletedTask;
    }
}
