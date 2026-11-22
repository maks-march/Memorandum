namespace Memorandum.Application.Abstractions;

/// <summary>Отправка писем. Application не знает, консоль это или SMTP.</summary>
public interface IEmailSender
{
    Task SendPasswordResetCodeAsync(
        string email,
        long userId,
        string code,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);
}
