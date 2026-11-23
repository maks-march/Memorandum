using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Memorandum.Application.Abstractions;
using Memorandum.Application.Invitations;
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

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Настройки проверяются при сборке контейнера, а не при первом запросе:
        // приложение должно падать на старте, а не на первом обращении к БД.
        var connectionString = ResolveConnection(configuration);

        services.AddDbContext<AppDbContext>(options => ConfigureProvider(options, connectionString));

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
        services.AddInvitations(configuration);

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

    /// <summary>
    /// Коды приглашения живут в Redis сутки: одноразовые, сами истекают,
    /// таблица в БД им не нужна. Соединение то же, что у сброса пароля.
    /// </summary>
    private static void AddInvitations(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<InvitationOptions>(configuration.GetSection(InvitationOptions.SectionName));
        services.AddSingleton<IInviteCodeStore, RedisInviteCodeStore>();
    }

    /// <summary>Провайдер БД один — Postgres, и для dev, и для прода: схема не расходится.</summary>
    private static string ResolveConnection(IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"] ?? Postgres;

        if (!string.Equals(provider, Postgres, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Поддерживается только Database:Provider = {Postgres}, получено «{provider}».");

        var connectionString = configuration.GetConnectionString(Postgres);

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Не задана строка подключения ConnectionStrings:Postgres (переменная ConnectionStrings__Postgres).");

        return connectionString;
    }

    private static void ConfigureProvider(DbContextOptionsBuilder options, string connectionString) =>
        options.UseNpgsql(connectionString);
}
