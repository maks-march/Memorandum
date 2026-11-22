using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Memorandum.Application.Abstractions;

namespace Memorandum.Api.Auth;

internal static class CookieAuthenticationExtensions
{
    /// <summary>
    /// Cookie-аутентификация для API: без редиректов на HTML (401/403), с проверкой
    /// <c>SecurityStamp</c> на каждом запросе — блокировка и смена пароля отзывают старые cookie.
    /// </summary>
    public static IServiceCollection AddApiCookieAuthentication(
        this IServiceCollection services,
        IHostEnvironment environment)
    {
        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = AuthCookie.Name(environment);
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                // Локально API работает по HTTP: без SameAsRequest браузер не сохранит Secure-cookie.
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                // __Host- требует Path=/ и запрещает Domain — оба условия выполняются по умолчанию.
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
                options.SlidingExpiration = false;

                options.Events.OnRedirectToLogin = context =>
                    WriteProblemAsync(context, StatusCodes.Status401Unauthorized, "Требуется вход",
                        "Cookie не передана или истекла.");

                options.Events.OnRedirectToAccessDenied = context =>
                    WriteProblemAsync(context, StatusCodes.Status403Forbidden, "Недостаточно прав",
                        "Роль не позволяет выполнить операцию.");

                options.Events.OnValidatePrincipal = async context =>
                {
                    var requestServices = context.HttpContext.RequestServices;
                    var users = requestServices.GetRequiredService<IUserRepository>();
                    var logger = requestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger(typeof(CookieAuthenticationExtensions));

                    var rawUserId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var stamp = context.Principal?.FindFirst(ClaimsPrincipalFactory.SecurityStampClaim)?.Value;

                    if (!long.TryParse(rawUserId, out var userId))
                    {
                        await RejectAsync(context);
                        return;
                    }

                    var user = await users.GetByIdAsync(userId, context.HttpContext.RequestAborted);

                    if (user is null
                        || !user.IsActive
                        || !string.Equals(user.SecurityStamp, stamp, StringComparison.Ordinal))
                    {
                        logger.LogInformation("Сессия отозвана: userId={UserId}, активен={IsActive}", userId, user?.IsActive);
                        await RejectAsync(context);
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }

    private static async Task WriteProblemAsync(
        RedirectContext<CookieAuthenticationOptions> context,
        int status,
        string title,
        string detail)
    {
        context.Response.StatusCode = status;

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
        };

        await context.Response.WriteAsJsonAsync(
            problem,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web),
            "application/problem+json");
    }

    private static async Task RejectAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
