using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Memorandum.Application.Auth;
using Memorandum.Application.PasswordChange;
using Memorandum.Application.PasswordChange.Models;
using Memorandum.Application.PasswordChange.Validators;
using Memorandum.Application.CurrentUser;
using Memorandum.Application.CurrentUser.Models;
using Memorandum.Application.CurrentUser.Validators;

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
        services.AddScoped<IPasswordChangeService, PasswordChangeService>();

        services.AddScoped<IValidator<IRegisterRequest>, RegisterRequestValidator>();
        services.AddScoped<IValidator<RegisterRequest>, RegisterWithOrganizationValidator>();
        services.AddScoped<IValidator<LoginRequest>, LoginRequestValidator>();
        services.AddScoped<IValidator<UpdateProfileRequest>, UpdateProfileRequestValidator>();
        services.AddScoped<IValidator<RequestPasswordChangeCodeRequest>, RequestPasswordChangeCodeRequestValidator>();
        services.AddScoped<IValidator<(long UserId, string? Code, string? NewPassword)>, ConfirmPasswordChangeRequestValidator>();

        return services;
    }
}
