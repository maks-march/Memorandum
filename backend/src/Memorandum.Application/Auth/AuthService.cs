using FluentValidation;
using Memorandum.Application.Abstractions;
using Memorandum.Application.Common;
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
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator) : IAuthService
{
    public async Task<Result<AuthUserDto>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await registerValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return validation.ToError().ToResult<AuthUserDto>();

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var author = currentUser.Name;

        User user;
        Organization? organization = null;

        try
        {
            var normalizedEmail = EmailNormalizer.Normalize(request.Email);

            if (await users.ExistsByNormalizedEmailAsync(normalizedEmail, cancellationToken))
                return EmailDuplicate().ToResult<AuthUserDto>();

            user = User.Create(request.Email, request.FullName, now, author);

            if (!string.IsNullOrWhiteSpace(request.OrgName))
            {
                organization = Organization.Create(request.OrgName, now, author);
                organization.AddMember(user, OrganizationRole.OrgAdmin, now, author);
                user.SetActiveOrganization(organization, now, author);
            }
        }
        catch (DomainException exception)
        {
            return Error.FromDomainException(exception).ToResult<AuthUserDto>();
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
