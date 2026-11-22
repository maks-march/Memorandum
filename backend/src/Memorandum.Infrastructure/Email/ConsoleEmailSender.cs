using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Memorandum.Application.Abstractions;

namespace Memorandum.Infrastructure.Email;

/// <summary>Заглушка почты: письмо печатается в лог. Только вне Production.</summary>
public sealed class ConsoleEmailSender(
    IHostEnvironment environment,
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
                "ConsoleEmailSender работает только вне Production: нужен SMTP-адаптер.");

        logger.LogInformation(
            "ПИСЬМО (заглушка): кому {Email}, userId {UserId}, " +
            "POST /api/v1/auth/change-password/{UserId}?code={Code}, действует до {ExpiresAt:O}",
            email, userId, userId, code, expiresAt);

        return Task.CompletedTask;
    }
}
