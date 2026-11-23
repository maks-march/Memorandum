using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Memorandum.Api.Contracts;
using Memorandum.Domain.Organizations;
using Memorandum.Tests.Infrastructure;
using Memorandum.Tests.Infrastructure.Api;

namespace Memorandum.Tests.Api;

/// <summary>
/// Код приглашения: выдаёт администратор организации, сотрудник регистрируется по нему
/// и попадает в ту же организацию. Код одноразовый и живёт сутки (хранится в Redis).
/// </summary>
public sealed class InviteCodeTests : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _fixture;

    public InviteCodeTests(ApiFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Admin_issues_code_and_employee_joins_organization()
    {
        var admin = await SignedInAdminAsync();
        var code = await admin.IssueInviteCodeAsync();

        Assert.Equal(InviteCodeLength, code.Code.Length);
        Assert.Equal("Editor", code.Role);
        Assert.Equal("ООО Ромашка", code.OrganizationName);
        Assert.True(code.ExpiresAt > DateTimeOffset.UtcNow.AddHours(23));

        var employee = await Session.StartAsync(_fixture.Client);
        var email = UniqueEmail("employee");
        var employeeId = await employee.AcceptInviteAsync(code.Code.ToLowerInvariant(), email);

        Assert.True(employeeId > 0);

        // Сотрудник видит организацию администратора и роль из кода.
        var login = await employee.LoginAsync(email);
        Assert.Equal("ООО Ромашка", login.User.OrganizationName);
        Assert.Equal(OrganizationRole.Editor.ToString(), login.User.Role!.ToString());

        using var members = await admin.GetAsync("/api/v1/me");
        Assert.Equal(HttpStatusCode.OK, members.StatusCode);
    }

    [Fact]
    public async Task Code_is_single_use()
    {
        var admin = await SignedInAdminAsync();
        var code = await admin.IssueInviteCodeAsync();

        await (await Session.StartAsync(_fixture.Client)).AcceptInviteAsync(code.Code, UniqueEmail("first"));

        var second = await Session.StartAsync(_fixture.Client);
        using var response = await second.PostAsync(
            "/api/v1/auth/accept-invite",
            new AcceptInviteHttpRequest(code.Code, UniqueEmail("second"), "password123", "Второй"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);

        var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal("INVITE_CODE_INVALID", problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task New_code_replaces_previous_one()
    {
        var admin = await SignedInAdminAsync();
        var stale = await admin.IssueInviteCodeAsync();
        var fresh = await admin.IssueInviteCodeAsync();

        Assert.NotEqual(stale.Code, fresh.Code);

        using var response = await (await Session.StartAsync(_fixture.Client)).PostAsync(
            "/api/v1/auth/accept-invite",
            new AcceptInviteHttpRequest(stale.Code, UniqueEmail(), "password123", "Поздний"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Admin_role_cannot_be_granted_by_code()
    {
        var admin = await SignedInAdminAsync();

        using var response = await admin.PostAsync(
            "/api/v1/auth/invite-code",
            new RequestInviteCodeHttpRequest(OrganizationRole.OrgAdmin));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Code_requires_authorization()
    {
        var anonymous = await Session.StartAsync(_fixture.Client);

        using var response = await anonymous.PostAsync(
            "/api/v1/auth/invite-code",
            new RequestInviteCodeHttpRequest(OrganizationRole.Editor));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Editor_without_organization_rights_cannot_issue_code()
    {
        // Редактор организации тоже не выдаёт коды: это право администратора.
        var admin = await SignedInAdminAsync();
        var code = await admin.IssueInviteCodeAsync();

        var editor = await Session.StartAsync(_fixture.Client);
        var email = UniqueEmail("editor");
        await editor.AcceptInviteAsync(code.Code, email);
        await editor.LoginAsync(email);

        using var response = await editor.PostAsync(
            "/api/v1/auth/invite-code",
            new RequestInviteCodeHttpRequest(OrganizationRole.Viewer));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Accept_invite_validates_form_and_code()
    {
        var session = await Session.StartAsync(_fixture.Client);

        using var withoutCode = await session.PostAsync(
            "/api/v1/auth/accept-invite",
            new AcceptInviteHttpRequest(null, UniqueEmail(), "password123", "Иван"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, withoutCode.StatusCode);

        using var invalidForm = await session.PostAsync(
            "/api/v1/auth/accept-invite",
            new AcceptInviteHttpRequest("ABCD2345", "нет-собаки", "short", " "));

        Assert.Equal(HttpStatusCode.BadRequest, invalidForm.StatusCode);
    }

    private static string UniqueEmail(string role = "admin") =>
        $"{role}-{Guid.NewGuid():N}@example.ru";

    private const int InviteCodeLength = 8;

    /// <summary>Администратор своей организации: он и выдаёт коды.</summary>
    private async Task<Session> SignedInAdminAsync()
    {
        var session = await Session.StartAsync(_fixture.Client);
        var email = UniqueEmail();

        await session.RegisterAsync(email, "ООО Ромашка");
        await session.LoginAsync(email);

        return session;
    }
}
