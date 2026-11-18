using Microsoft.EntityFrameworkCore;
using Memorandum.Domain.Common;
using Memorandum.Domain.Organizations;
using Memorandum.Domain.Users;
using Memorandum.Infrastructure.Persistence.Repositories;

namespace Memorandum.Tests.Infrastructure;

public sealed class UnitOfWorkTests
{
    [Fact]
    public async Task Save_maps_duplicate_email_to_domain_conflict()
    {
        await using var database = await SqliteDatabase.CreateAsync();
        var unitOfWork = new UnitOfWork(database.Context);
        var now = TestHelpers.Now;

        database.Context.Users.Add(User.Create("a@b.ru", "Первый", now));
        await unitOfWork.SaveChangesAsync();

        database.Context.Users.Add(User.Create("A@B.RU", "Второй", now));

        var exception = await Assert.ThrowsAsync<ConflictException>(() => unitOfWork.SaveChangesAsync());
        Assert.Equal(ErrorCodes.EmailDuplicate, exception.Code);
    }

    [Fact]
    public async Task Save_maps_second_active_membership_to_domain_conflict()
    {
        await using var database = await SqliteDatabase.CreateAsync();
        var unitOfWork = new UnitOfWork(database.Context);
        var now = TestHelpers.Now;

        var user = User.Create("a@b.ru", "Иван", now);
        var first = Organization.Create("Первая", now);
        var second = Organization.Create("Вторая", now);
        first.AddMember(user, OrganizationRole.Editor, now);
        second.AddMember(user, OrganizationRole.Editor, now);

        database.Context.Organizations.AddRange(first, second);

        // Частичный уникальный индекс: активное участие может быть только в одной организации.
        var exception = await Assert.ThrowsAsync<ConflictException>(() => unitOfWork.SaveChangesAsync());

        Assert.Equal(ErrorCodes.ReferenceInUse, exception.Code);
    }

    [Fact]
    public async Task Inactive_membership_does_not_block_readding_user()
    {
        await using var database = await SqliteDatabase.CreateAsync();
        var unitOfWork = new UnitOfWork(database.Context);
        var now = TestHelpers.Now;

        var user = User.Create("a@b.ru", "Иван", now);
        var organization = Organization.Create("ООО", now);
        var membership = organization.AddMember(user, OrganizationRole.Editor, now);

        database.Context.Organizations.Add(organization);
        await unitOfWork.SaveChangesAsync();

        organization.RemoveMember(user.Id, now);
        await unitOfWork.SaveChangesAsync();

        await using var verification = database.NewContext();
        var saved = await verification.Memberships.AsNoTracking().SingleAsync();
        Assert.False(saved.IsActive);
        Assert.Equal(membership.Id, saved.Id);
    }
}
