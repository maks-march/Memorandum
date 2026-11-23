using Microsoft.Extensions.Options;
using Moq;
using Memorandum.Application.Abstractions;
using Memorandum.Application.Invitations;
using Memorandum.Application.Invitations.Models;
using Memorandum.Application.Invitations.Validators;
using Memorandum.Domain.Common;
using Memorandum.Domain.Interfaces;
using Memorandum.Domain.Organizations;

namespace Memorandum.Tests.Application;

public sealed class InviteCodeServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IOrganizationRepository> _organizations = new();
    private readonly Mock<IInviteCodeStore> _store = new();
    private readonly InvitationOptions _options = new() { CodePepper = "pepper" };

    private InviteCodeService CreateService(long? userId = 5, OrganizationRole role = OrganizationRole.OrgAdmin) =>
        new(
            _organizations.Object,
            _store.Object,
            new FakeCurrentUser(userId, "admin"),
            new TestTimeProvider(Now),
            Options.Create(_options),
            new RequestInviteCodeRequestValidator());

    private void GivenOrganization(long id = 7, string name = "ООО Ромашка", bool isActive = true)
    {
        _organizations
            .Setup(o => o.GetActiveMembershipAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ActiveMembership(id, name, OrganizationRole.OrgAdmin, "ГИП"));

        var organization = Organization.Create(name, Now.UtcDateTime).WithId(id);

        if (!isActive)
            organization.Deactivate(Now.UtcDateTime);

        _organizations
            .Setup(o => o.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);
    }

    [Fact]
    public async Task Admin_gets_code_that_lives_a_day()
    {
        GivenOrganization();
        InviteCodeEntry? saved = null;
        _store
            .Setup(s => s.SaveAsync(It.IsAny<InviteCodeEntry>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Callback<InviteCodeEntry, string, TimeSpan, CancellationToken>((entry, _, _, _) => saved = entry)
            .Returns(Task.CompletedTask);

        var result = await CreateService().IssueAsync(new RequestInviteCodeRequest(OrganizationRole.Editor));

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value.OrganizationId);
        Assert.Equal("ООО Ромашка", result.Value.OrganizationName);
        Assert.Equal(InviteCode.Length, result.Value.Code.Length);
        Assert.Equal(OrganizationRole.Editor, result.Value.Role);
        Assert.Equal(Now.AddDays(1), result.Value.ExpiresAt);

        // В хранилище уходит хеш, а не сам код.
        Assert.NotNull(saved);
        Assert.NotEqual(result.Value.Code, saved!.CodeHash);
        Assert.Equal(InviteCode.Hash(_options.CodePepper, result.Value.Code), saved.CodeHash);
    }

    [Fact]
    public async Task Non_admin_gets_forbidden()
    {
        _organizations
            .Setup(o => o.GetActiveMembershipAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ActiveMembership(7, "ООО Ромашка", OrganizationRole.Editor, null));

        var result = await CreateService().IssueAsync(new RequestInviteCodeRequest(OrganizationRole.Editor));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.Forbidden, result.Error.Code);
        _store.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task User_without_organization_gets_forbidden()
    {
        _organizations
            .Setup(o => o.GetActiveMembershipAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ActiveMembership?)null);

        var result = await CreateService().IssueAsync(new RequestInviteCodeRequest(OrganizationRole.Editor));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.Forbidden, result.Error.Code);
    }

    [Fact]
    public async Task Anonymous_request_requires_login()
    {
        var result = await CreateService(userId: null)
            .IssueAsync(new RequestInviteCodeRequest(OrganizationRole.Editor));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.InvalidCredentials, result.Error.Code);
    }

    [Fact]
    public async Task Admin_role_is_rejected_by_validator()
    {
        var result = await CreateService().IssueAsync(new RequestInviteCodeRequest(OrganizationRole.OrgAdmin));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.Validation, result.Error.Code);
        Assert.Contains("Role", result.Error.FieldErrors!.Keys);
        _organizations.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Inactive_organization_cannot_invite()
    {
        GivenOrganization(isActive: false);

        var result = await CreateService().IssueAsync(new RequestInviteCodeRequest(OrganizationRole.Viewer));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.OrganizationInactive, result.Error.Code);
    }

    [Fact]
    public async Task Storage_failure_is_reported_as_unavailable()
    {
        GivenOrganization();
        _store
            .Setup(s => s.SaveAsync(It.IsAny<InviteCodeEntry>(), It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("redis down"));

        var result = await CreateService().IssueAsync(new RequestInviteCodeRequest(OrganizationRole.Editor));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.StorageUnavailable, result.Error.Code);
    }
}
