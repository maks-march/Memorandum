using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Memorandum.Application.Abstractions;
using Memorandum.Application.PasswordChange;
using Memorandum.Domain.Interfaces;
using Memorandum.Infrastructure.Email;
using Memorandum.Infrastructure.Identity;
using Memorandum.Infrastructure.Persistence;
using Memorandum.Infrastructure.Redis;
using StackExchange.Redis;
using Memorandum.Infrastructure.Persistence.DataProtection;
using Memorandum.Infrastructure.Persistence.Repositories;
using Memorandum.Infrastructure.Security;

namespace Memorandum.Infrastructure;

/// <summary>Регистрация доступа к данным и адаптеров к платформе.</summary>
public static class DependencyInjection
{
    public const string Postgres = "Postgres";
    public const string Sqlite = "Sqlite";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Настройки проверяются при сборке контейнера, а не при первом запросе:
        // приложение должно падать на старте, а не на первом обращении к БД.
        var connection = ResolveConnection(configuration);

        services.AddDbContext<AppDbContext>(options => ConfigureProvider(options, connection));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<ICredentialRepository, CredentialRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Ключи Data Protection — в БД и с общим именем приложения: иначе после деплоя
        // cookie и XSRF-токены «сами» перестают проверяться.
        services.AddDataProtection().SetApplicationName("memorandum");
        services.AddOptions<KeyManagementOptions>()
            .Configure<IServiceScopeFactory>((options, scopeFactory) =>
                options.XmlRepository = new EfXmlRepository(scopeFactory));

        services.AddSingleton<IPasswordHasher, AspNetCorePasswordHasher>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        services.AddPasswordReset(configuration);

        return services;
    }

    /// <summary>
    /// Сброс пароля живёт в Redis: тикеты с TTL и атомарная проверка кода.
    /// Соединение — синглтон; <c>abortConnect=false</c>, чтобы недоступный Redis
    /// не валил старт сервиса целиком.
    /// </summary>
    private static void AddPasswordReset(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PasswordResetOptions>(configuration.GetSection(PasswordResetOptions.SectionName));

        var redisConfiguration = configuration["Redis:Configuration"];

        if (string.IsNullOrWhiteSpace(redisConfiguration))
            throw new InvalidOperationException(
                "Не задана Redis:Configuration — коды смены пароля хранятся в Redis (переменная Redis__Configuration).");

        var options = ConfigurationOptions.Parse(redisConfiguration);
        options.AbortOnConnectFail = false;
        options.ConnectRetry = 3;
        options.ConnectTimeout = 5000;

        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(options));
        services.AddSingleton<IPasswordResetTicketStore, RedisPasswordResetTicketStore>();
        services.AddScoped<IEmailSender, ConsoleEmailSender>();
    }

    private static (string Provider, string ConnectionString) ResolveConnection(IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"] ?? Sqlite;

        if (string.Equals(provider, Postgres, StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = configuration.GetConnectionString(Postgres);

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException(
                    "Для Database:Provider = Postgres нужна строка ConnectionStrings:Postgres.");

            return (Postgres, connectionString);
        }

        if (string.Equals(provider, Sqlite, StringComparison.OrdinalIgnoreCase))
            return (Sqlite, configuration.GetConnectionString(Sqlite) ?? "Data Source=app.db");

        throw new InvalidOperationException(
            $"Неизвестный Database:Provider = «{provider}». Допустимо: {Sqlite}, {Postgres}.");
    }

    private static void ConfigureProvider(
        DbContextOptionsBuilder options,
        (string Provider, string ConnectionString) connection)
    {
        if (connection.Provider == Postgres)
            options.UseNpgsql(connection.ConnectionString);
        else
            options.UseSqlite(connection.ConnectionString);
    }
}
