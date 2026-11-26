using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Memorandum.Application.Abstractions;
using Memorandum.Domain.Interfaces;
using Memorandum.Infrastructure;
using Memorandum.Infrastructure.Email;
using Memorandum.Infrastructure.Persistence;

namespace Memorandum.Tests.Infrastructure;

public sealed class DependencyInjectionTests
{
    private static ServiceProvider BuildProvider(
        Dictionary<string, string?> settings,
        string environment = "Development")
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        var hostEnvironment = new TestHostEnvironment { EnvironmentName = environment };

        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(hostEnvironment);
        services.AddInfrastructure(configuration, hostEnvironment);

        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    [Fact]
    public void Postgres_provider_registers_all_ports()
    {
        using var provider = BuildProvider(new()
        {
            ["Database:Provider"] = "Postgres",
            ["ConnectionStrings:Postgres"] = "Host=localhost;Database=memorandum;Username=u;Password=p",
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
    public void Removed_provider_fails_with_clear_message()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            BuildProvider(new()
            {
                ["Database:Provider"] = "Sqlite",
                ["ConnectionStrings:Postgres"] = "Host=localhost;Database=memorandum;Username=u;Password=p",
                ["Redis:Configuration"] = "localhost:6379,abortConnect=false",
            }));

        Assert.Contains("Postgres", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Missing_redis_configuration_fails_with_clear_message()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            BuildProvider(new()
            {
                ["Database:Provider"] = "Postgres",
                ["ConnectionStrings:Postgres"] = "Host=localhost;Database=memorandum;Username=u;Password=p",
            }));

        Assert.Contains("Redis:Configuration", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Without_smtp_outside_production_email_goes_to_log()
    {
        using var provider = BuildProvider(MinimalSettings());
        using var scope = provider.CreateScope();

        Assert.IsType<ConsoleEmailSender>(scope.ServiceProvider.GetRequiredService<IEmailSender>());
    }

    [Fact]
    public void Production_without_smtp_fails_with_clear_message()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            BuildProvider(MinimalSettings(), environment: "Production"));

        Assert.Contains("Smtp:Host", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Configured_smtp_is_used_outside_production_too()
    {
        var settings = MinimalSettings();
        settings["Smtp:Host"] = "smtp.example.ru";
        settings["Smtp:From"] = "no-reply@example.ru";

        using var provider = BuildProvider(settings);
        using var scope = provider.CreateScope();

        Assert.IsType<SmtpEmailSender>(scope.ServiceProvider.GetRequiredService<IEmailSender>());
    }

    [Fact]
    public void Smtp_host_without_from_fails_with_clear_message()
    {
        var settings = MinimalSettings();
        settings["Smtp:Host"] = "smtp.example.ru";

        var exception = Assert.Throws<InvalidOperationException>(() => BuildProvider(settings));

        Assert.Contains("Smtp:From", exception.Message, StringComparison.Ordinal);
    }

    private static Dictionary<string, string?> MinimalSettings() => new()
    {
        ["Database:Provider"] = "Postgres",
        ["ConnectionStrings:Postgres"] = "Host=localhost;Database=memorandum;Username=u;Password=p",
        ["Redis:Configuration"] = "localhost:6379,abortConnect=false",
    };
}

/// <summary>Окружение без файловой системы: нужно только имя.</summary>
internal sealed class TestHostEnvironment : IHostEnvironment
{
    public string EnvironmentName { get; set; } = "Development";
    public string ApplicationName { get; set; } = "Memorandum.Tests";
    public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
