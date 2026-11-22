using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Memorandum.Domain.Organizations;
using Memorandum.Domain.Users;
using Memorandum.Infrastructure.Persistence;
using Memorandum.Infrastructure.Persistence.DataProtection;

namespace Memorandum.Tests.Infrastructure;

/// <summary>
/// Проверяет описание модели без обращения к БД: контекст создаётся с провайдером Postgres,
/// соединение не открывается.
/// </summary>
public sealed class ModelTests
{
    private static AppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=memorandum;Username=u;Password=p")
            .Options);

    [Fact]
    public void Model_contains_aggregate_roots()
    {
        using var db = NewContext();

        Assert.NotNull(db.Model.FindEntityType(typeof(User)));
        Assert.NotNull(db.Model.FindEntityType(typeof(Organization)));
        Assert.NotNull(db.Model.FindEntityType(typeof(OrganizationMembership)));
        Assert.NotNull(db.Model.FindEntityType(typeof(Credential)));
    }

    [Theory]
    [InlineData(typeof(User), "users")]
    [InlineData(typeof(Organization), "organizations")]
    [InlineData(typeof(OrganizationMembership), "organization_memberships")]
    [InlineData(typeof(Credential), "credentials")]
    public void Tables_are_snake_case(Type entityType, string tableName)
    {
        using var db = NewContext();

        Assert.Equal(tableName, db.Model.FindEntityType(entityType)!.GetTableName());
    }

    [Fact]
    public void User_key_is_generated_by_database()
    {
        using var db = NewContext();
        var key = db.Model.FindEntityType(typeof(User))!.FindPrimaryKey()!;

        Assert.Equal(nameof(User.Id), Assert.Single(key.Properties).Name);
        Assert.Equal(ValueGenerated.OnAdd, key.Properties[0].ValueGenerated);
    }

    [Fact]
    public void NormalizedEmail_index_is_unique()
    {
        using var db = NewContext();
        var index = db.Model.FindEntityType(typeof(User))!
            .GetIndexes()
            .Single(i => i.Properties.Single().Name == nameof(User.NormalizedEmail));

        Assert.True(index.IsUnique);
        Assert.Equal("ux_users_normalized_email", index.GetDatabaseName());
        Assert.Null(index.GetFilter());
    }

    [Fact]
    public void Email_columns_are_bounded()
    {
        using var db = NewContext();
        var entity = db.Model.FindEntityType(typeof(User))!;

        Assert.Equal(User.EmailMaxLength, entity.FindProperty(nameof(User.Email))!.GetMaxLength());
        Assert.Equal(User.EmailMaxLength, entity.FindProperty(nameof(User.NormalizedEmail))!.GetMaxLength());
        Assert.Equal(User.FullNameMaxLength, entity.FindProperty(nameof(User.FullName))!.GetMaxLength());
    }

    [Fact]
    public void Membership_index_is_unique_only_for_active_rows()
    {
        using var db = NewContext();
        var index = db.Model.FindEntityType(typeof(OrganizationMembership))!
            .GetIndexes()
            .Single(i => i.Properties.Single().Name == nameof(OrganizationMembership.UserId));

        Assert.True(index.IsUnique);
        Assert.Equal("\"is_active\"", index.GetFilter());
        Assert.Equal("ux_organization_memberships_user_id_active", index.GetDatabaseName());
    }

    [Fact]
    public void Role_is_stored_as_string()
    {
        using var db = NewContext();
        var property = db.Model.FindEntityType(typeof(OrganizationMembership))!
            .FindProperty(nameof(OrganizationMembership.Role))!;

        Assert.Equal(typeof(string), property.GetProviderClrType());
        Assert.Equal("character varying(16)", property.GetColumnType());
    }

    [Fact]
    public void Memberships_use_backing_field()
    {
        using var db = NewContext();
        var navigation = db.Model.FindEntityType(typeof(Organization))!
            .FindNavigation(nameof(Organization.Memberships))!;

        Assert.Equal("_memberships", navigation.FieldInfo?.Name);
        Assert.Equal(PropertyAccessMode.Field, navigation.GetPropertyAccessMode());
    }

    [Theory]
    [InlineData("fk_users_active_organization", DeleteBehavior.SetNull)]
    [InlineData("fk_organization_memberships_users", DeleteBehavior.Cascade)]
    [InlineData("fk_organization_memberships_organizations", DeleteBehavior.Cascade)]
    [InlineData("fk_credentials_users", DeleteBehavior.Cascade)]
    public void Foreign_keys_have_delete_behaviour(string constraintName, DeleteBehavior expected)
    {
        using var db = NewContext();

        var foreignKey = db.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetForeignKeys())
            .Single(key => key.GetConstraintName() == constraintName);

        Assert.Equal(expected, foreignKey.DeleteBehavior);
    }

    [Fact]
    public void All_string_columns_have_max_length()
    {
        using var db = NewContext();

        var unbounded = db.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetProperties())
            // XML ключа Data Protection — единственное строковое поле без лимита: его длина задана форматом ключа.
            .Where(property => property.ClrType == typeof(string)
                && property.GetMaxLength() is null
                && property.DeclaringType.ClrType != typeof(DataProtectionKey))
            .Select(property => $"{property.DeclaringType.Name}.{property.Name}")
            .ToArray();

        Assert.Empty(unbounded);
    }
}
