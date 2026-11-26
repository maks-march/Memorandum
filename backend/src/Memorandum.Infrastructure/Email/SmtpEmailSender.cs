using System.Globalization;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Memorandum.Application.Abstractions;

namespace Memorandum.Infrastructure.Email;

/// <summary>Отправка писем по SMTP.</summary>
/// <remarks>
/// Одно соединение на письмо: писем мало и они редкие, а долгоживущий клиент требует
/// своего переподключения. Секреты в лог не попадают — код есть только в теле письма,
/// в логе остаются адрес и тема.
/// </remarks>
public sealed class SmtpEmailSender(
    IOptions<SmtpOptions> options,
    EmailFallback fallback,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private SmtpOptions Options => options.Value;

    public Task SendPasswordResetCodeAsync(
        string email,
        long userId,
        string code,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            email,
            "Код для смены пароля",
            $"""
             Здравствуйте!

             Код для смены пароля: {code}
             Код действует до {Format(expiresAt)}.

             Если вы не запрашивали смену пароля, просто не отвечайте на письмо.
             """,
            cancellationToken);

    private async Task SendAsync(string email, string subject, string body, CancellationToken cancellationToken)
    {
        // Конфигурация проверяется на старте (AddEmail), здесь — только грубая страховка.
        if (string.IsNullOrWhiteSpace(Options.Host) || string.IsNullOrWhiteSpace(Options.From))
            throw new InvalidOperationException(
                "SMTP не настроен: нужны Smtp:Host и Smtp:From (переменные Smtp__Host, Smtp__From).");

        using var message = new MailMessage
        {
            From = new MailAddress(Options.From),
            Subject = subject,
            Body = body.ReplaceLineEndings("\n"),
            SubjectEncoding = System.Text.Encoding.UTF8,
            BodyEncoding = System.Text.Encoding.UTF8,
        };

        message.To.Add(new MailAddress(email));

        using var client = new SmtpClient(Options.Host, Options.Port)
        {
            EnableSsl = Options.UseStartTls,
            Timeout = (int)Options.Timeout.TotalMilliseconds,
            DeliveryMethod = SmtpDeliveryMethod.Network,
        };

        // Пустой логин — анонимный сервер (MailHog, локальный релей).
        if (!string.IsNullOrWhiteSpace(Options.User))
            client.Credentials = new NetworkCredential(Options.User, Options.Password);

        try
        {
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // В лог — без тела письма: в нём код.
            logger.LogError(
                exception,
                "Не удалось отправить письмо по SMTP. Кому={Email}, тема={Subject}, сервер={Host}:{Port}",
                email, subject, Options.Host, Options.Port);

            if (!fallback.Allowed)
                throw;

            // dev без доступного SMTP: письмо не должно пропасть вместе с кодом.
            fallback.Write(logger, email, subject, body);
        }

        logger.LogInformation("Письмо отправлено. Кому={Email}, тема={Subject}", email, subject);
    }

    private static string Format(DateTimeOffset moment) =>
        moment.ToString("dd.MM.yyyy HH:mm 'UTC'", CultureInfo.InvariantCulture);
}
