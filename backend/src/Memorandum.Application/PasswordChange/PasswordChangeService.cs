using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Memorandum.Application.Abstractions;
using Memorandum.Application.Common;
using Memorandum.Application.PasswordChange.Models;
using Memorandum.Domain.Common;
using Memorandum.Domain.Interfaces;
using Memorandum.Domain.Users;

namespace Memorandum.Application.PasswordChange;

public sealed class PasswordChangeService(
    IUserRepository users,
    ICredentialRepository credentials,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IPasswordResetTicketStore tickets,
    IEmailSender emailSender,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IOptions<PasswordResetOptions> options,
    IValidator<RequestPasswordChangeCodeRequest> requestValidator,
    IValidator<(long UserId, string? Code, string? NewPassword)> confirmValidator,
    ILogger<PasswordChangeService> logger) : IPasswordChangeService
{
    private PasswordResetOptions Options => options.Value;

    public async Task<Result> RequestCodeAsync(
        RequestPasswordChangeCodeRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await requestValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result.Failure(validation.ToError());

        var user = await users.GetByNormalizedEmailAsync(
            EmailNormalizer.Normalize(request.Email), cancellationToken);

        // Неизвестный email неотличим от известного: ни письма, ни иного ответа.
        if (user is null)
            return Result.Success();

        if (!await tickets.TryStartCooldownAsync(user.Id, Options.ResendCooldown, cancellationToken))
            return Result.Failure(Error.Business(
                ErrorCodes.ResetTooManyRequests,
                "Код уже отправлен. Повторите запрос позже."));

        var now = timeProvider.GetUtcNow();
        var code = PasswordResetCode.Generate();

        var ticket = new PasswordResetTicket(
            CodeHash: PasswordResetCode.Hash(Options.CodePepper, user.Id, code),
            AttemptsLeft: Options.MaxAttempts,
            ExpiresAt: now.Add(Options.CodeTimeToLive),
            IssuedAt: now,
            Email: user.Email);

        await tickets.SaveAsync(user.Id, ticket, Options.CodeTimeToLive, cancellationToken);
        await emailSender.SendPasswordResetCodeAsync(
            user.Email, user.Id, code, ticket.ExpiresAt, cancellationToken);

        // Сам код в лог не пишем: в Development его печатает ConsoleEmailSender.
        logger.LogInformation(
            "Код смены пароля выдан. UserId={UserId}, действует до {ExpiresAt}", user.Id, ticket.ExpiresAt);

        return Result.Success();
    }

    public async Task<Result> ConfirmAsync(
        long userId,
        string? code,
        ConfirmPasswordChangeRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await confirmValidator.ValidateAsync(
            (userId, code, request.NewPassword), cancellationToken);

        if (!validation.IsValid)
            return Result.Failure(validation.ToError());

        // Пользователя заранее не ищем: для несуществующего userId тикета просто нет,
        // и ответ совпадает с «код не найден» — перечислить пользователей нельзя.
        var codeHash = PasswordResetCode.Hash(Options.CodePepper, userId, code!);
        var consumed = await tickets.ConsumeCodeAsync(userId, codeHash, cancellationToken);

        switch (consumed.Status)
        {
            case ConsumeCodeStatus.NotFound:
                return Result.Failure(Error.Business(
                    ErrorCodes.ResetTicketNotFound,
                    "Код не найден или уже использован. Запросите новый."));

            case ConsumeCodeStatus.Expired:
                return Result.Failure(Error.Business(
                    ErrorCodes.ResetCodeExpired,
                    "Срок действия кода истёк. Запросите новый."));

            case ConsumeCodeStatus.Exhausted:
                return Result.Failure(Error.Business(
                    ErrorCodes.ResetAttemptsExhausted,
                    "Попытки исчерпаны. Запросите новый код."));

            case ConsumeCodeStatus.Invalid:
                return Result.Failure(Error.Business(
                    ErrorCodes.ResetCodeInvalid,
                    $"Неверный код. Осталось попыток: {consumed.AttemptsLeft}.",
                    new Dictionary<string, object> { ["attemptsLeft"] = consumed.AttemptsLeft }));
        }

        var user = await users.GetForUpdateAsync(userId, cancellationToken);

        if (user is null || !user.IsActive)
            return Result.Failure(Error.Business(
                ErrorCodes.ResetTicketNotFound,
                "Код не найден или уже использован. Запросите новый."));

        var credential = await credentials.GetByUserIdAsync(userId, cancellationToken);

        if (credential is null)
            return Result.Failure(Error.Business(
                ErrorCodes.InvalidCredentials,
                "Учётные данные не найдены. Обратитесь к администратору."));

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var author = currentUser.Name;

        credential.ChangeHash(passwordHasher.Hash(request.NewPassword!), now, author);
        user.ChangeSecurityStamp(now, author);

        var saved = await unitOfWork.TrySaveAsync(cancellationToken);
        if (saved.IsFailure)
            return Result.Failure(saved.Error);

        logger.LogInformation("Пароль изменён. UserId={UserId}", userId);
        return Result.Success();
    }
}
