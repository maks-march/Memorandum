using Microsoft.EntityFrameworkCore;
using Memorandum.Domain.Organizations;
using Memorandum.Domain.Users;
using Memorandum.Infrastructure.Persistence.Repositories;

namespace Memorandum.Tests.Infrastructure;

/// <summary>Сохранение и чтение агрегатов на Postgres: та же схема, что и в проде.</summary>
public sealed class RepositoryTests
{
    [Fact]
    public async Task Saves_organization_with_members_through_public_api()
    {
        await using var database = await PostgresTestDatabase.CreateAsync();
        var now = TestHelpers.Now;

        var organization = Organization.Create("ООО Ромашка", now, "admin");
        var user = User.Create("a@b.ru", "Иван", now, "admin");
        organization.AddMember(user, OrganizationRole.OrgAdmin, now, "admin");

        database.Context.Organizations.Add(organization);
        await database.Context.SaveChangesAsync();

        await using var verification = database.NewContext();
        var saved = await new OrganizationRepository(verification).GetByIdAsync(organization.Id);

        Assert.NotNull(saved);
        Assert.NotEqual(0, saved!.Id);
        Assert.Equal("ООО Ромашка", saved.Name);
        var membership = Assert.Single(saved.Memberships);
        Assert.Equal(OrganizationRole.OrgAdmin, membership.Role);
        Assert.NotEqual(0, membership.UserId);
        Assert.Equal(now, saved.CreatedAt);
    }

    [Fact]
    public async Task User_keeps_private_setters_round_trip()
    {
        await using var database = await PostgresTestDatabase.CreateAsync();
        var user = User.Create("Ivan@Example.RU", "Иван", TestHelpers.Now, "admin");

        database.Context.Users.Add(user);
        await database.Context.SaveChangesAsync();

        await using var verification = database.NewContext();
        var saved = await new UserRepository(verification).GetByNormalizedEmailAsync("ivan@example.ru");

        Assert.NotNull(saved);
        Assert.NotEqual(0, saved!.Id);
        Assert.Equal("Ivan@Example.RU", saved.Email);
        Assert.Equal("Иван", saved.FullName);
        Assert.True(saved.IsActive);
        Assert.Equal(user.SecurityStamp, saved.SecurityStamp);
    }

    [Fact]
    public async Task Credential_is_stored_for_user()
    {
        await using var database = await PostgresTestDatabase.CreateAsync();
        var user = User.Create("a@b.ru", "Иван", TestHelpers.Now);

        database.Context.Users.Add(user);
        database.Context.Credentials.Add(Credential.Create(user, "hash-1", TestHelpers.Now));
        await database.Context.SaveChangesAsync();

        await using var verification = database.NewContext();
        var saved = await new CredentialRepository(verification).GetByUserIdAsync(user.Id);

        Assert.NotNull(saved);
        Assert.Equal("hash-1", saved!.PasswordHash);
    }

    [Fact]
    public async Task Active_membership_is_resolved_for_user()
    {
        await using var database = await PostgresTestDatabase.CreateAsync();
        var user = User.Create("a@b.ru", "Иван", TestHelpers.Now);
        var organization = Organization.Create("ООО Ромашка", TestHelpers.Now);
        organization.AddMember(user, OrganizationRole.Editor, TestHelpers.Now);
        user.SetActiveOrganization(organization, TestHelpers.Now);

        database.Context.Organizations.Add(organization);
        database.Context.Users.Add(user);
        await database.Context.SaveChangesAsync();

        await using var verification = database.NewContext();
        var membership = await new OrganizationRepository(verification)
            .GetActiveMembershipAsync(user.Id);

        Assert.NotNull(membership);
        Assert.Equal(organization.Id, membership!.OrganizationId);
        Assert.Equal("ООО Ромашка", membership.OrganizationName);
        Assert.Equal(OrganizationRole.Editor, membership.Role);
    }

    [Fact]
    public async Task Active_membership_is_null_without_selected_organization()
    {
        await using var database = await PostgresTestDatabase.CreateAsync();
        var user = User.Create("a@b.ru", "Иван", TestHelpers.Now);

        database.Context.Users.Add(user);
        await database.Context.SaveChangesAsync();

        await using var verification = database.NewContext();

        Assert.Null(await new OrganizationRepository(verification).GetActiveMembershipAsync(user.Id));
    }

    [Fact]
    public async Task ExistsByNormalizedEmail_finds_saved_user()
    {
        await using var database = await PostgresTestDatabase.CreateAsync();
        database.Context.Users.Add(User.Create("A@B.RU", "Иван", TestHelpers.Now));
        await database.Context.SaveChangesAsync();

        await using var verification = database.NewContext();
        var repository = new UserRepository(verification);

        Assert.True(await repository.ExistsByNormalizedEmailAsync("a@b.ru"));
        Assert.False(await repository.ExistsByNormalizedEmailAsync("other@b.ru"));
    }

    [Fact]
    public async Task Deleting_organization_removes_memberships()
    {
        await using var database = await PostgresTestDatabase.CreateAsync();
        var user = User.Create("a@b.ru", "Иван", TestHelpers.Now);
        var organization = Organization.Create("ООО", TestHelpers.Now);
        organization.AddMember(user, OrganizationRole.Editor, TestHelpers.Now);

        database.Context.Organizations.Add(organization);
        await database.Context.SaveChangesAsync();

        database.Context.Organizations.Remove(organization);
        await database.Context.SaveChangesAsync();

        await using var verification = database.NewContext();
        Assert.Empty(await verification.Memberships.ToListAsync());
        Assert.Single(await verification.Users.ToListAsync());
    }
}
