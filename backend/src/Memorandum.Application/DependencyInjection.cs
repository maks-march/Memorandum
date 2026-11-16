using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Memorandum.Application.Auth;
using Memorandum.Application.CurrentUser;

namespace Memorandum.Application;

/// <summary>
/// Регистрация сервисов и валидаторов слоя Application.
/// Репозитории, хешер паролей и <see cref="Abstractions.ICurrentUser"/> регистрирует Infrastructure.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddScoped<IValidator<RegisterRequest>, RegisterRequestValidator>();
        services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();

        return services;
    }
}
