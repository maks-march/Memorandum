using FluentValidation;
using Moq;
using Memorandum.Application.Abstractions;
using Memorandum.Application.CurrentUser;
using Memorandum.Application.CurrentUser.Models;
using Memorandum.Application.CurrentUser.Validators;
using Memorandum.Domain.Interfaces;
using Memorandum.Domain.Common;
using Memorandum.Domain.Organizations;
using Memorandum.Domain.Users;

namespace Memorandum.Tests.Application;

public sealed class CurrentUserServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IOrganizationRepository> _organizations = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly IValidator<UpdateProfileRequest> _validator = new UpdateProfileRequestValidator();

    private CurrentUserService CreateService(ICurrentUser currentUser) =>
        new(
            _users.Object,
            _organizations.Object,
            _unitOfWork.Object,
            currentUser,
            new TestTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)),
            _validator);

    [Fact]
    public async Task Get_rejects_anonymous_request()
    {
        var result = await CreateService(new FakeCurrentUser()).GetAsync();

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.InvalidCredentials, result.Error.Code);
        _users.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Get_returns_not_found_for_missing_user()
    {
        _users.Setup(u => u.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        var result = await CreateService(new FakeCurrentUser(5, "Иван")).GetAsync();

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.NotFound, result.Error.Code);
    }

    [Fact]
    public async Task Get_rejects_inactive_user()
    {
        var user = User.Create("a@b.ru", "Иван", TestHelpers.Now).WithId(5);
        user.Deactivate(TestHelpers.Now);
        _users.Setup(u => u.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var result = await CreateService(new FakeCurrentUser(5, "Иван")).GetAsync();

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.UserInactive, result.Error.Code);
    }

    [Fact]
    public async Task Get_returns_profile_without_organization()
    {
        var user = User.Create("a@b.ru", "Иван", TestHelpers.Now).WithId(5);
        _users.Setup(u => u.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _organizations
            .Setup(o => o.GetActiveMembershipAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ActiveMembership?)null);

        var result = await CreateService(new FakeCurrentUser(5, "Иван")).GetAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Id);
        Assert.Equal("a@b.ru", result.Value.Email);
        Assert.Null(result.Value.OrganizationId);
        Assert.Null(result.Value.OrganizationName);
        Assert.Null(result.Value.Role);
    }

    [Fact]
    public async Task Get_returns_profile_with_organization_and_role()
    {
        var user = User.Create("a@b.ru", "Иван", TestHelpers.Now).WithId(5);
        _users.Setup(u => u.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _organizations
            .Setup(o => o.GetActiveMembershipAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ActiveMembership(9, "ООО Ромашка", OrganizationRole.OrgAdmin, "Инженер"));

        var result = await CreateService(new FakeCurrentUser(5, "Иван")).GetAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(9, result.Value.OrganizationId);
        Assert.Equal("ООО Ромашка", result.Value.OrganizationName);
        Assert.Equal(OrganizationRole.OrgAdmin, result.Value.Role);
    }
}
