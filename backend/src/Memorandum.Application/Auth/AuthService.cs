using FluentValidation;
using Memorandum.Application.Abstractions;
using Memorandum.Application.Common;
using Memorandum.Application.Invitations;
using Memorandum.Domain.Common;
using Memorandum.Domain.Interfaces;
using Memorandum.Domain.Organizations;
using Memorandum.Domain.Users;

namespace Memorandum.Application.Auth;

public sealed class AuthService(
    IUserRepository users,
    IOrganizationRepository organizations,
    ICredentialRepository credentials,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ICurrentUser currentUser,
    TimeProvider timeProvider,
    IInviteCodeStore inviteCodes,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator) : IAuthService
{
    public async Task<Result<AuthUserDto>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await registerValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result<AuthUserDto>.Failure(validation.ToError());

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var author = currentUser.Name;

        User user;
        Organization? organization = null;

        try
        {
            var normalizedEmail = EmailNormalizer.Normalize(request.Email);

            if (await users.ExistsByNormalizedEmailAsync(normalizedEmail, cancellationToken))
                return Result<AuthUserDto>.Failure(EmailDuplicate());

            user = User.Create(request.Email, request.FullName, now, author);

            // Создатель организации — единственный, кто получает её без кода
            // приглашения: он и есть источник кодов для остальных сотрудников.
            if (!string.IsNullOrWhiteSpace(request.OrgName))
            {
                organization = Organization.Create(request.OrgName, now, author);
                organization.AddMember(user, OrganizationRole.OrgAdmin, now, author);
                user.SetActiveOrganization(organization, now, author);
            }
        }
        catch (DomainException exception)
        {
            return Result<AuthUserDto>.Failure(Error.FromDomainException(exception));
        }

        var passwordHash = passwordHasher.Hash(request.Password!);

        await users.AddAsync(user, cancellationToken);
        await credentials.AddAsync(Credential.Create(user, passwordHash, now, author), cancellationToken);

        if (organization is not null)
            await organizations.AddAsync(organization, cancellationToken);

        var saved = await unitOfWork.TrySaveAsync(cancellationToken);
        if (saved.IsFailure)
        {
            // Нарушение уникальности индекса по NormalizedEmail — это тот же дубликат.
            return Result<AuthUserDto>.Failure(
                saved.Error.Code == ErrorCodes.EmailDuplicate ? EmailDuplicate() : saved.Error);
        }

        return Result<AuthUserDto>.Success(AuthUserDto.From(user, organization?.Name));
    }

    public async Task<Result<AuthUserDto>> RegisterByInviteAsync(
        RegisterRequest request,
        string? code,
        CancellationToken cancellationToken = default)
    {
        var validation = await registerValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result<AuthUserDto>.Failure(validation.ToError());

        if (string.IsNullOrWhiteSpace(code))
            return Result<AuthUserDto>.Failure(Error.Business(
                ErrorCodes.InviteCodeMissing,
                "Укажите код приглашения: без него регистрация сотрудника невозможна."));

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var author = currentUser.Name;

        User user;

        try
        {
            var normalizedEmail = EmailNormalizer.Normalize(request.Email);

            if (await users.ExistsByNormalizedEmailAsync(normalizedEmail, cancellationToken))
                return Result<AuthUserDto>.Failure(EmailDuplicate());

            user = User.Create(request.Email, request.FullName, now, author);
        }
        catch (DomainException exception)
        {
            return Result<AuthUserDto>.Failure(Error.FromDomainException(exception));
        }

        // Код гасим до создания пользователя: он одноразовый, и «съеденный»
        // код не должен оставаться действительным при сбое сохранения.
        InviteCodeEntry? invite;

        try
        {
            invite = await inviteCodes.RedeemAsync(code, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return Result<AuthUserDto>.Failure(Error.Business(
                ErrorCodes.StorageUnavailable,
                "Сервис временно недоступен: не удалось проверить код приглашения."));
        }

        if (invite is null)
            return Result<AuthUserDto>.Failure(Error.Business(
                ErrorCodes.InviteCodeInvalid,
                "Код приглашения недействителен, истёк или уже использован."));

        var invitedTo = await organizations.GetByIdAsync(invite.OrganizationId, cancellationToken);

        if (invitedTo is null)
            return Result<AuthUserDto>.Failure(Error.NotFound("Организация", invite.OrganizationId));

        try
        {
            // Одна организация у пользователя: если он уже где-то состоит,
            // приглашение не должно молча перевести его в другую.
            invitedTo.AddMember(user, invite.Role, now, author);
            user.SetActiveOrganization(invitedTo, now, author);
        }
        catch (DomainException exception)
        {
            return Result<AuthUserDto>.Failure(Error.FromDomainException(exception));
        }

        var passwordHash = passwordHasher.Hash(request.Password!);

        await users.AddAsync(user, cancellationToken);
        await credentials.AddAsync(Credential.Create(user, passwordHash, now, author), cancellationToken);

        var saved = await unitOfWork.TrySaveAsync(cancellationToken);
        if (saved.IsFailure)
        {
            return Result<AuthUserDto>.Failure(
                saved.Error.Code == ErrorCodes.EmailDuplicate ? EmailDuplicate() : saved.Error);
        }

        return Result<AuthUserDto>.Success(AuthUserDto.From(user, invitedTo.Name));
    }

    public async Task<Result<LoginOutcome>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await loginValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return Result<LoginOutcome>.Failure(validation.ToError());

        var normalizedEmail = EmailNormalizer.Normalize(request.Email);
        var user = await users.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);

        if (user is null)
            return Result<LoginOutcome>.Failure(InvalidCredentials());

        var credential = await credentials.GetByUserIdAsync(user.Id, cancellationToken);
        if (credential is null)
            return Result<LoginOutcome>.Failure(InvalidCredentials());

        var verification = passwordHasher.Verify(credential.PasswordHash, request.Password!);

        if (verification == PasswordVerificationResult.Failed || !user.IsActive)
            return Result<LoginOutcome>.Failure(InvalidCredentials());

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;
            credential.ChangeHash(passwordHasher.Hash(request.Password!), now, currentUser.Name);

            var saved = await unitOfWork.TrySaveAsync(cancellationToken);
            if (saved.IsFailure)
                return Result<LoginOutcome>.Failure(saved.Error);
        }

        return Result<LoginOutcome>.Success(
            new LoginOutcome(user.Id, user.SecurityStamp, user.Email, user.FullName));
    }

    /// <summary>Одинаковый ответ на все причины отказа: существование учётной записи не раскрывается.</summary>
    private static Error InvalidCredentials() =>
        Error.Business(ErrorCodes.InvalidCredentials, "Неверный email или пароль.");

    private static Error EmailDuplicate() =>
        Error.Business(ErrorCodes.EmailDuplicate, "Пользователь с таким email уже зарегистрирован.");
}
