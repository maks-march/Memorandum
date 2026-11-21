using FluentValidation;
using FluentValidation.Results;
using Moq;
using Memorandum.Application.Abstractions;
using Memorandum.Application.Auth;
using Memorandum.Application.Common;
using Memorandum.Application.PasswordChange;
using Memorandum.Application.PasswordChange.Models;
using Memorandum.Application.PasswordChange.Validators;
using Memorandum.Domain.Common;
using Memorandum.Domain.Users;
using Memorandum.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Memorandum.Tests.Application;

public sealed class PasswordChangeServiceTests
{
    private const string Pepper = "test-pepper";
    private static readonly DateTime Now = TestHelpers.Now;

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICredentialRepository> _credentials = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly FakePasswordHasher _hasher = new();
    private readonly FakeTicketStore _tickets = new();
    private readonly FakeEmailSender _emails = new();
    private readonly PasswordResetOptions _options = new()
    {
        MaxAttempts = 3,
        CodeTimeToLive = TimeSpan.FromMinutes(15),
        ResendCooldown = TimeSpan.FromSeconds(60),
        CodePepper = Pepper,
    };

    public PasswordChangeServiceTests()
    {
        _unitOfWork.Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private PasswordChangeService CreateService(IValidator<(long, string?, string?)>? confirmValidator = null) =>
        new(
            _users.Object,
            _credentials.Object,
            _unitOfWork.Object,
            _hasher,
            _tickets,
            _emails,
            new FakeCurrentUser(name: "admin"),
            new TestTimeProvider(new DateTimeOffset(Now, TimeSpan.Zero)),
            Options.Create(_options),
            new RequestPasswordChangeCodeRequestValidator(),
            confirmValidator ?? new ConfirmPasswordChangeRequestValidator(),
            NullLogger<PasswordChangeService>.Instance);

    private static User ExistingUser(long id = 7) =>
        User.Create("a@b.ru", "Иван", Now).WithId(id);

    private static ConfirmPasswordChangeRequest NewPassword(string password = "password123") => new(password);

    [Fact]
    public async Task Request_unknown_email_returns_success_and_sends_nothing()
    {
        _users.Setup(u => u.GetByNormalizedEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var result = await CreateService().RequestCodeAsync(new RequestPasswordChangeCodeRequest("nobody@b.ru"));

        Assert.True(result.IsSuccess);
        Assert.Empty(_emails.Sent);
        Assert.Null(_tickets.SavedTicket);
    }

    [Fact]
    public async Task Request_known_email_saves_hashed_ticket_and_sends_code()
    {
        var user = ExistingUser();
        _users.Setup(u => u.GetByNormalizedEmailAsync("a@b.ru", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await CreateService().RequestCodeAsync(new RequestPasswordChangeCodeRequest(" A@B.RU "));

        Assert.True(result.IsSuccess);

        var ticket = Assert.IsType<PasswordResetTicket>(_tickets.SavedTicket);
        Assert.Equal(3, ticket.AttemptsLeft);
        Assert.Equal(Now.AddMinutes(15), ticket.ExpiresAt.UtcDateTime);
        Assert.Equal("a@b.ru", ticket.Email);

        var email = Assert.Single(_emails.Sent);
        Assert.Equal(PasswordResetCode.Hash(Pepper, user.Id, email.Code), ticket.CodeHash);
        Assert.Matches("^[0-9]{6}$", email.Code);
    }

    [Fact]
    public async Task Request_saves_only_code_hash()
    {
        var user = ExistingUser();
        _users.Setup(u => u.GetByNormalizedEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await CreateService().RequestCodeAsync(new RequestPasswordChangeCodeRequest("a@b.ru"));

        var code = Assert.Single(_emails.Sent).Code;
        Assert.DoesNotContain(code, _tickets.SavedTicket!.CodeHash, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Request_within_cooldown_returns_too_many_requests()
    {
        var user = ExistingUser();
        _users.Setup(u => u.GetByNormalizedEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var service = CreateService();
        await service.RequestCodeAsync(new RequestPasswordChangeCodeRequest("a@b.ru"));
        var second = await service.RequestCodeAsync(new RequestPasswordChangeCodeRequest("a@b.ru"));

        Assert.True(second.IsFailure);
        Assert.Equal(ErrorCodes.ResetTooManyRequests, second.Error.Code);
        Assert.Single(_emails.Sent);
    }

    [Fact]
    public async Task Request_rejects_invalid_email()
    {
        var result = await CreateService().RequestCodeAsync(new RequestPasswordChangeCodeRequest("нет-собаки"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.Validation, result.Error.Code);
        _users.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Confirm_correct_code_changes_hash_and_rotates_stamp()
    {
        var user = ExistingUser();
        var credential = Credential.Create(user, "old-hash", Now);
        var stamp = user.SecurityStamp;
        var code = await IssueCodeAsync(user);

        _users.Setup(u => u.GetForUpdateAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _credentials.Setup(c => c.GetByUserIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(credential);

        var result = await CreateService().ConfirmAsync(user.Id, code, NewPassword());

        Assert.True(result.IsSuccess);
        Assert.Equal(FakePasswordHasher.Prefix + "password123", credential.PasswordHash);
        Assert.NotEqual(stamp, user.SecurityStamp);
        Assert.Null(_tickets.SavedTicket);
        _unitOfWork.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Confirm_wrong_code_decrements_attempts()
    {
        var user = ExistingUser();
        var code = await IssueCodeAsync(user);
        var wrong = code == "000000" ? "111111" : "000000";
        _users.Setup(u => u.GetForUpdateAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var service = CreateService();
        var first = await service.ConfirmAsync(user.Id, wrong, NewPassword());
        var second = await service.ConfirmAsync(user.Id, wrong, NewPassword());

        Assert.True(first.IsFailure);
        Assert.Equal(ErrorCodes.ResetCodeInvalid, first.Error.Code);
        Assert.Equal(2, first.Error.Details!["attemptsLeft"]);
        Assert.Equal(1, second.Error.Details!["attemptsLeft"]);
    }

    [Fact]
    public async Task Confirm_third_wrong_attempt_deletes_ticket()
    {
        var user = ExistingUser();
        var code = await IssueCodeAsync(user);
        var wrong = code == "000000" ? "111111" : "000000";
        _users.Setup(u => u.GetForUpdateAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var service = CreateService();
        for (var attempt = 1; attempt <= 2; attempt++)
            await service.ConfirmAsync(user.Id, wrong, NewPassword());

        var third = await service.ConfirmAsync(user.Id, wrong, NewPassword());

        Assert.Equal(ErrorCodes.ResetAttemptsExhausted, third.Error.Code);
        Assert.Null(_tickets.SavedTicket);
    }

    [Fact]
    public async Task Confirm_unknown_user_returns_ticket_not_found()
    {
        var result = await CreateService().ConfirmAsync(999, "123456", NewPassword());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.ResetTicketNotFound, result.Error.Code);
    }

    [Fact]
    public async Task Confirm_expired_ticket_returns_expired()
    {
        var user = ExistingUser();
        var code = await IssueCodeAsync(user);
        _tickets.Expire(user.Id);

        var result = await CreateService().ConfirmAsync(user.Id, code, NewPassword());

        Assert.Equal(ErrorCodes.ResetCodeExpired, result.Error.Code);
    }

    [Fact]
    public async Task Confirm_weak_password_returns_validation()
    {
        var user = ExistingUser();
        var code = await IssueCodeAsync(user);

        var result = await CreateService().ConfirmAsync(user.Id, code, NewPassword("short"));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.Validation, result.Error.Code);
        Assert.True(result.Error.FieldErrors!.ContainsKey("NewPassword"));
    }

    [Fact]
    public async Task Confirm_consumes_ticket_only_once()
    {
        var user = ExistingUser();
        var credential = Credential.Create(user, "old-hash", Now);
        var code = await IssueCodeAsync(user);
        _users.Setup(u => u.GetForUpdateAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _credentials.Setup(c => c.GetByUserIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(credential);

        var service = CreateService();
        var first = await service.ConfirmAsync(user.Id, code, NewPassword());
        var second = await service.ConfirmAsync(user.Id, code, NewPassword());

        Assert.True(first.IsSuccess);
        Assert.Equal(ErrorCodes.ResetTicketNotFound, second.Error.Code);
    }

    private async Task<string> IssueCodeAsync(User user)
    {
        _users.Setup(u => u.GetByNormalizedEmailAsync(user.NormalizedEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await CreateService().RequestCodeAsync(new RequestPasswordChangeCodeRequest(user.Email));
        return _emails.Sent[^1].Code;
    }
}
