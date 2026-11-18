using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Memorandum.Application.Auth;

namespace Memorandum.Api.Auth;

/// <summary>Claim-набор сессии: id, email, стамп для проверки актуальности cookie.</summary>
internal static class ClaimsPrincipalFactory
{
    /// <summary>Стамп, по которому сессия отзывается после блокировки или смены пароля.</summary>
    public const string SecurityStampClaim = "sstamp";

    public static ClaimsPrincipal Create(LoginOutcome outcome) =>
        new(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, outcome.UserId.ToString()),
                new Claim(ClaimTypes.Name, outcome.FullName),
                new Claim(ClaimTypes.Email, outcome.Email),
                new Claim(SecurityStampClaim, outcome.SecurityStamp),
            ],
            CookieAuthenticationDefaults.AuthenticationScheme));
}
