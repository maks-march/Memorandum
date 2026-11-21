using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Memorandum.Application.Abstractions;
using Memorandum.Domain.Interfaces;
using Memorandum.Infrastructure;
using Memorandum.Infrastructure.Persistence;

namespace Memorandum.Tests.Infrastructure;

public sealed class DependencyInjectionTests
{
    private static ServiceProvider BuildProvider(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddInfrastructure(configuration);

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    [Fact]
    public void Sqlite_provider_registers_all_ports()
    {
        using var provider = BuildProvider(new()
        {
            ["Database:Provider"] = "Sqlite",
            ["ConnectionStrings:Sqlite"] = "Data Source=:memory:",
            ["Redis:Configuration"] = "localhost:6379,abortConnect=false",
        });
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<AppDbContext>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IUserRepository>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IOrganizationRepository>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICredentialRepository>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IUnitOfWork>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ICurrentUser>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IPasswordHasher>());
    }

    [Fact]
    public void Postgres_provider_registers_context()
    {
        using var provider = BuildProvider(new()
        {
            ["Database:Provider"] = "Postgres",
            ["ConnectionStrings:Postgres"] = "Host=localhost;Database=memorandum;Username=u;Password=p",
            ["Redis:Configuration"] = "localhost:6379,abortConnect=false",
        });
        using var scope = provider.CreateScope();

        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL",
            scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.ProviderName);
    }

    [Fact]
    public void Postgres_without_connection_string_fails_with_clear_message()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            BuildProvider(new()
            {
                ["Database:Provider"] = "Postgres",
                ["Redis:Configuration"] = "localhost:6379,abortConnect=false",
            }));

        Assert.Contains("ConnectionStrings:Postgres", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Unknown_provider_fails_with_clear_message()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            BuildProvider(new()
            {
                ["Database:Provider"] = "Oracle",
                ["Redis:Configuration"] = "localhost:6379,abortConnect=false",
            }));

        Assert.Contains("Oracle", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Missing_redis_configuration_fails_with_clear_message()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            BuildProvider(new()
            {
                ["Database:Provider"] = "Sqlite",
                ["ConnectionStrings:Sqlite"] = "Data Source=:memory:",
            }));

        Assert.Contains("Redis:Configuration", exception.Message, StringComparison.Ordinal);
    }
}
