using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Memorandum.Api.Domain.Organizations;
using Memorandum.Api.Domain.Users;
using Memorandum.Api.Infrastructure;

namespace Memorandum.Tests.Infrastructure;

public sealed class ModelTests
{
    private static MemorandumDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<MemorandumDbContext>()
            .UseNpgsql("Host=localhost;Database=x;Username=u;Password=p")
            .Options);

    [Fact]
    public void Model_contains_both_aggregate_roots()
    {
        using var db = CreateContext();

        Assert.NotNull(db.Model.FindEntityType(typeof(User)));
        Assert.NotNull(db.Model.FindEntityType(typeof(Organization)));
    }

    [Fact]
    public void Membership_is_mapped_but_not_a_root()
    {
        using var db = CreateContext();

        Assert.NotNull(db.Model.FindEntityType(typeof(OrganizationMembership)));
        Assert.DoesNotContain(typeof(MemorandumDbContext).GetProperties(),
            p => p.PropertyType == typeof(DbSet<OrganizationMembership>));
    }

    [Fact]
    public void User_key_is_generated_on_add()
    {
        using var db = CreateContext();

        var user = db.Model.FindEntityType(typeof(User))!;
        var key = Assert.Single(user.GetKeys());

        Assert.True(key.IsPrimaryKey());
        var id = Assert.Single(key.Properties);
        Assert.Equal(nameof(User.Id), id.Name);
        Assert.Equal(ValueGenerated.OnAdd, id.ValueGenerated);
    }

    [Fact]
    public void NormalizedEmail_is_unique_and_bounded()
    {
        using var db = CreateContext();

        var user = db.Model.FindEntityType(typeof(User))!;
        var email = user.FindProperty(nameof(User.NormalizedEmail))!;

        Assert.Equal("normalized_email", email.GetColumnName());
        Assert.Equal(254, email.GetMaxLength());
        Assert.Contains(user.GetIndexes(),
            index => index.IsUnique && index.Properties.Count == 1 && index.Properties[0] == email);
    }

    [Fact]
    public void Membership_unique_index_is_filtered_by_active()
    {
        using var db = CreateContext();

        var membership = db.Model.FindEntityType(typeof(OrganizationMembership))!;
        var index = Assert.Single(membership.GetIndexes(),
            i => i.Properties.Any(p => p.Name == nameof(OrganizationMembership.UserId)));

        Assert.True(index.IsUnique);
        Assert.Equal("\"is_active\"", index.GetFilter());
    }

    [Fact]
    public void Role_is_stored_as_string()
    {
        using var db = CreateContext();

        var role = db.Model.FindEntityType(typeof(OrganizationMembership))!
            .FindProperty(nameof(OrganizationMembership.Role))!;

        Assert.Equal("character varying(16)", role.GetColumnType());
        Assert.NotNull(role.GetTypeMapping().Converter);
    }

    [Fact]
    public void Organization_memberships_use_backing_field()
    {
        using var db = CreateContext();

        var organization = db.Model.FindEntityType(typeof(Organization))!;
        var navigation = organization.FindNavigation(nameof(Organization.Memberships))!;

        Assert.Equal("_memberships", navigation.FieldInfo?.Name);
        Assert.Equal(PropertyAccessMode.Field, navigation.GetPropertyAccessMode());
    }

    [Fact]
    public void Foreign_keys_have_expected_delete_behaviour()
    {
        using var db = CreateContext();

        var membership = db.Model.FindEntityType(typeof(OrganizationMembership))!;
        var membershipFks = membership.GetForeignKeys()
            .ToDictionary(fk => Assert.Single(fk.Properties).Name);

        Assert.Equal(DeleteBehavior.Cascade,
            membershipFks[nameof(OrganizationMembership.UserId)].DeleteBehavior);
        Assert.Equal(DeleteBehavior.Cascade,
            membershipFks[nameof(OrganizationMembership.OrganizationId)].DeleteBehavior);

        var user = db.Model.FindEntityType(typeof(User))!;
        var activeOrganizationFk = Assert.Single(user.GetForeignKeys());
        Assert.Equal(nameof(User.ActiveOrganizationId), Assert.Single(activeOrganizationFk.Properties).Name);
        Assert.Equal(DeleteBehavior.SetNull, activeOrganizationFk.DeleteBehavior);
    }

    [Fact]
    public void Tables_and_columns_are_snake_case()
    {
        using var db = CreateContext();

        Assert.Equal("users", db.Model.FindEntityType(typeof(User))!.GetTableName());
        Assert.Equal("organizations", db.Model.FindEntityType(typeof(Organization))!.GetTableName());
        Assert.Equal("organization_memberships",
            db.Model.FindEntityType(typeof(OrganizationMembership))!.GetTableName());

        Assert.Equal("normalized_email",
            db.Model.FindEntityType(typeof(User))!.FindProperty(nameof(User.NormalizedEmail))!.GetColumnName());
        Assert.Equal("active_organization_id",
            db.Model.FindEntityType(typeof(User))!.FindProperty(nameof(User.ActiveOrganizationId))!.GetColumnName());
        Assert.Equal("organization_id",
            db.Model.FindEntityType(typeof(OrganizationMembership))!
                .FindProperty(nameof(OrganizationMembership.OrganizationId))!.GetColumnName());
    }

    [Fact]
    public void All_strings_have_max_length()
    {
        using var db = CreateContext();

        foreach (var property in db.Model.GetEntityTypes().SelectMany(t => t.GetProperties()))
        {
            var isString = property.ClrType == typeof(string) ||
                           property.GetValueConverter()?.ProviderClrType == typeof(string);

            if (isString)
                Assert.NotNull(property.GetMaxLength());
        }
    }
}
