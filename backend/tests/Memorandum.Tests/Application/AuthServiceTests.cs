using FluentValidation;
using Moq;
using Memorandum.Application.Abstractions;
using Memorandum.Application.Auth;
using Memorandum.Application.Invitations;
using Memorandum.Domain.Common;
using Memorandum.Domain.Organizations;
using Memorandum.Domain.Users;
using Memorandum.Domain.Interfaces;

namespace Memorandum.Tests.Application;

public sealed class AuthServiceTests
{
    private static readonly DateTime Now = TestHelpers.Now;

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IOrganizationRepository> _organizations = new();
    private readonly Mock<ICredentialRepository> _credentials = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly FakePasswordHasher _hasher = new();
    private readonly FakeCurrentUser _currentUser = new(name: "admin");
    private readonly Mock<IInviteCodeStore> _inviteCodes = new();

    public AuthServiceTests()
    {
        _unitOfWork.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private AuthService CreateService(
        IValidator<RegisterRequest>? registerValidator = null,
        IValidator<LoginRequest>? loginValidator = null,
        FakeCurrentUser? currentUser = null) =>
        new(
            _users.Object,
            _organizations.Object,
            _credentials.Object,
            _unitOfWork.Object,
            _hasher,
            currentUser ?? _currentUser,
            new TestTimeProvider(new DateTimeOffset(Now, TimeSpan.Zero)),
            _inviteCodes.Object,
            registerValidator ?? new RegisterRequestValidator(),
            loginValidator ?? new LoginRequestValidator());

    private static RegisterRequest ValidRequest(string? orgName = null) =>
        new("Ivan@Example.RU", "password123", "Иван Петров", orgName);

    [Fact]
    public async Task Register_creates_user_without_organization()
    {
        _users.Setup(u => u.ExistsByNormalizedEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await CreateService().RegisterAsync(ValidRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal("Ivan@Example.RU", result.Value.Email);
        Assert.Null(result.Value.OrganizationName);

        _users.Verify(
            u => u.AddAsync(It.Is<User>(user =>
                user.NormalizedEmail == "ivan@example.ru" &&
                user.CreatedAt == Now &&
                user.UpdatedBy == "admin"), It.IsAny<CancellationToken>()),
            Times.Once);

        _organizations.Verify(
            o => o.AddAsync(It.IsAny<Organization>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Register_stores_only_password_hash()
    {
        Credential? stored = null;
        _users.Setup(u => u.ExistsByNormalizedEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _credentials
            .Setup(c => c.AddAsync(It.IsAny<Credential>(), It.IsAny<CancellationToken>()))
            .Callback<Credential, CancellationToken>((credential, _) => stored = credential)
            .Returns(Task.CompletedTask);

        await CreateService().RegisterAsync(ValidRequest());

        Assert.NotNull(stored);
        Assert.Equal(FakePasswordHasher.Prefix + "password123", stored!.PasswordHash);
        Assert.NotEqual("password123", stored.PasswordHash);
        Assert.Equal(Now, stored.CreatedAt);
    }

    [Fact]
    public async Task Register_with_organization_makes_creator_admin()
    {
        Organization? stored = null;
        _users.Setup(u => u.ExistsByNormalizedEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _organizations
            .Setup(o => o.AddAsync(It.IsAny<Organization>(), It.IsAny<CancellationToken>()))
            .Callback<Organization, CancellationToken>((organization, _) => stored = organization)
            .Returns(Task.CompletedTask);

        var result = await CreateService().RegisterAsync(ValidRequest("ООО Ромашка"));

        Assert.True(result.IsSuccess);
        Assert.Equal("ООО Ромашка", result.Value.OrganizationName);
        Assert.NotNull(stored);

        var membership = Assert.Single(stored!.Memberships);
        Assert.Equal(OrganizationRole.OrgAdmin, membership.Role);
        Assert.True(membership.IsActive);
        Assert.Equal(Now, stored.CreatedAt);
    }

    [Fact]
    public async Task Register_rejects_duplicate_email()
    {
        _users.Setup(u => u.ExistsByNormalizedEmailAsync("ivan@example.ru", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateService().RegisterAsync(ValidRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.EmailDuplicate, result.Error.Code);
        _users.Verify(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Register_rejects_invalid_request_without_touching_repositories()
    {
        var result = await CreateService().RegisterAsync(new RegisterRequest("нет-собаки", "short", " ", null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.Validation, result.Error.Code);
        Assert.NotNull(result.Error.FieldErrors);
        Assert.Contains("Email", result.Error.FieldErrors!.Keys);
        Assert.Contains("Password", result.Error.FieldErrors!.Keys);
        Assert.Contains("FullName", result.Error.FieldErrors!.Keys);
        _users.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Register_translates_domain_exception()
    {
        _users.Setup(u => u.ExistsByNormalizedEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var service = CreateService(registerValidator: new AlwaysValidValidator<RegisterRequest>());
        var result = await service.RegisterAsync(new RegisterRequest("", "password123", "Иван", null));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.Required, result.Error.Code);
        _users.Verify(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Register_returns_conflict_when_unique_index_breaks()
    {
        _users.Setup(u => u.ExistsByNormalizedEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _unitOfWork
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException(ErrorCodes.EmailDuplicate, "Уже существует."));

        var result = await CreateService().RegisterAsync(ValidRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.EmailDuplicate, result.Error.Code);
    }

    [Fact]
    public async Task Register_passes_single_moment_to_all_entities()
    {
        Organization? storedOrganization = null;
        User? storedUser = null;
        Credential? storedCredential = null;
        _users.Setup(u => u.ExistsByNormalizedEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _users
            .Setup(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => storedUser = user)
            .Returns(Task.CompletedTask);
        _credentials
            .Setup(c => c.AddAsync(It.IsAny<Credential>(), It.IsAny<CancellationToken>()))
            .Callback<Credential, CancellationToken>((credential, _) => storedCredential = credential)
            .Returns(Task.CompletedTask);

        _organizations
            .Setup(o => o.AddAsync(It.IsAny<Organization>(), It.IsAny<CancellationToken>()))
            .Callback<Organization, CancellationToken>((organization, _) => storedOrganization = organization)
            .Returns(Task.CompletedTask);

        await CreateService().RegisterAsync(ValidRequest("ООО"));

        Assert.Equal(Now, storedUser!.CreatedAt);
        Assert.Equal(Now, storedCredential!.CreatedAt);
        Assert.Equal(Now, storedOrganization!.CreatedAt);
        Assert.Equal(Now, Assert.Single(storedOrganization.Memberships).CreatedAt);
    }

    [Fact]
    public async Task RegisterByInvite_puts_user_into_invited_organization()
    {
        User? stored = null;
        _users.Setup(u => u.ExistsByNormalizedEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _users
            .Setup(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Callback<User, CancellationToken>((user, _) => stored = user)
            .Returns(Task.CompletedTask);

        _inviteCodes
            .Setup(s => s.RedeemAsync("ABCD2345", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InviteCodeEntry(7, "hash", OrganizationRole.Editor, DateTimeOffset.UtcNow.AddHours(1)));

        var organization = Organization.Create("ООО Ромашка", Now).WithId(7);
        _organizations
            .Setup(o => o.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);

        var result = await CreateService().RegisterByInviteAsync(ValidRequest(), "ABCD2345");

        Assert.True(result.IsSuccess);
        Assert.Equal("ООО Ромашка", result.Value.OrganizationName);
        Assert.NotNull(stored);
        // FK проставит EF после вставки: в домене связь держит навигация.
        Assert.Same(organization, stored!.ActiveOrganization);
        Assert.Equal(OrganizationRole.Editor, Assert.Single(organization.Memberships).Role);
    }

    [Fact]
    public async Task RegisterByInvite_requires_code()
    {
        var result = await CreateService().RegisterByInviteAsync(ValidRequest(), "  ");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.InviteCodeMissing, result.Error.Code);
        _users.VerifyNoOtherCalls();
        _inviteCodes.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RegisterByInvite_rejects_unknown_or_used_code()
    {
        _users.Setup(u => u.ExistsByNormalizedEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _inviteCodes
            .Setup(s => s.RedeemAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InviteCodeEntry?)null);

        var result = await CreateService().RegisterByInviteAsync(ValidRequest(), "ZZZZ9999");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.InviteCodeInvalid, result.Error.Code);
        _users.Verify(u => u.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterByInvite_reports_storage_failure()
    {
        _users.Setup(u => u.ExistsByNormalizedEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _inviteCodes
            .Setup(s => s.RedeemAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("redis down"));

        var result = await CreateService().RegisterByInviteAsync(ValidRequest(), "ABCD2345");

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.StorageUnavailable, result.Error.Code);
    }

    [Fact]
    public async Task Login_returns_outcome_for_valid_credentials()
    {
        var user = User.Create("a@b.ru", "Иван", Now).WithId(5);
        _users.Setup(u => u.GetByNormalizedEmailAsync("a@b.ru", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _credentials.Setup(c => c.GetByUserIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Credential.Create(user, FakePasswordHasher.Prefix + "password123", Now));

        var result = await CreateService().LoginAsync(new LoginRequest("A@B.RU", "password123"));

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.UserId);
        Assert.Equal(user.SecurityStamp, result.Value.SecurityStamp);
        _unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Login_rejects_unknown_user()
    {
        _users.Setup(u => u.GetByNormalizedEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await CreateService().LoginAsync(new LoginRequest("a@b.ru", "password123"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.InvalidCredentials, result.Error.Code);
    }

    [Fact]
    public async Task Login_rejects_wrong_password()
    {
        var user = User.Create("a@b.ru", "Иван", Now).WithId(5);
        _users.Setup(u => u.GetByNormalizedEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _credentials.Setup(c => c.GetByUserIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Credential.Create(user, FakePasswordHasher.Prefix + "other-password", Now));

        var result = await CreateService().LoginAsync(new LoginRequest("a@b.ru", "password123"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.InvalidCredentials, result.Error.Code);
    }

    [Fact]
    public async Task Login_rejects_inactive_user_with_same_error()
    {
        var user = User.Create("a@b.ru", "Иван", Now).WithId(5);
        user.Deactivate(Now);
        _users.Setup(u => u.GetByNormalizedEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _credentials.Setup(c => c.GetByUserIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Credential.Create(user, FakePasswordHasher.Prefix + "password123", Now));

        var result = await CreateService().LoginAsync(new LoginRequest("a@b.ru", "password123"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.InvalidCredentials, result.Error.Code);
    }

    [Fact]
    public async Task Login_rehashes_outdated_hash()
    {
        var user = User.Create("a@b.ru", "Иван", Now).WithId(5);
        var credential = Credential.Create(user, "old-format-hash", Now);
        _users.Setup(u => u.GetByNormalizedEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _credentials.Setup(c => c.GetByUserIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);
        _hasher.Result = PasswordVerificationResult.SuccessRehashNeeded;

        var result = await CreateService().LoginAsync(new LoginRequest("a@b.ru", "password123"));

        Assert.True(result.IsSuccess);
        Assert.Equal(FakePasswordHasher.Prefix + "password123", credential.PasswordHash);
        Assert.Equal(Now, credential.UpdatedAt);
        _unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Login_returns_failure_when_rehash_cannot_be_saved()
    {
        var user = User.Create("a@b.ru", "Иван", Now).WithId(5);
        _users.Setup(u => u.GetByNormalizedEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _credentials.Setup(c => c.GetByUserIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Credential.Create(user, "old-format-hash", Now));
        _hasher.Result = PasswordVerificationResult.SuccessRehashNeeded;
        _unitOfWork
            .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ConflictException(ErrorCodes.ReferenceInUse, "Конфликт."));

        var result = await CreateService().LoginAsync(new LoginRequest("a@b.ru", "password123"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.ReferenceInUse, result.Error.Code);
    }

    [Fact]
    public async Task Login_rejects_invalid_request()
    {
        var result = await CreateService().LoginAsync(new LoginRequest("", ""));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.Validation, result.Error.Code);
        _users.VerifyNoOtherCalls();
    }
}
