using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Memorandum.Domain.Common;

namespace Memorandum.Api.Infrastructure.Errors;

/// <summary>
/// Антифоржери на cookie без заголовка.
/// <para>
/// Штатная схема требует, чтобы токен пришёл <b>и</b> в cookie, <b>и</b> в заголовке
/// (или поле формы) — именно сравнение двух копий и защищает от подделки запроса
/// с чужого сайта. Если принимать только cookie, защита исчезает: браузер приложит
/// её к любому запросу, отправленному сторонним сайтом.
/// </para>
/// <para>
/// Компромисс: клиент не пишет заголовок руками — middleware сам копирует токен
/// из cookie в заголовок перед проверкой, но <b>только когда</b> cookie пришла и
/// запрос не помечен как межсайтовый (<c>Sec-Fetch-Site</c> не <c>cross-site</c>,
/// нет <c>Origin</c> с чужого домена). Так остаётся защита от CSRF и уходит
/// ручная работа на фронте.
/// </para>
/// </summary>
internal static class AntiforgeryCookieMiddleware
{
    /// <summary>Заголовок, в который перекладывается токен: имя пункта меню и старых клиентов не меняется.</summary>
    public const string HeaderName = "X-XSRF-TOKEN";

    public static WebApplication UseAntiforgeryCookie(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            if (!RequiresToken(context.Request))
            {
                await next();
                return;
            }

            var cookie = context.Request.Cookies[ServiceCollectionExtensions.AntiforgeryCookieName];

            if (!string.IsNullOrEmpty(cookie) && string.IsNullOrEmpty(context.Request.Headers[HeaderName]))
                context.Request.Headers[HeaderName] = cookie;

            await next();
        });

        return app;
    }

    /// <summary>
    /// Только небезопасные методы и только «свой» запрос. Признак межсайтового
    /// запроса даёт браузер: <c>Sec-Fetch-Site: cross-site</c> или чужой Origin.
    /// </summary>
    private static bool RequiresToken(HttpRequest request)
    {
        if (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method)
            || HttpMethods.IsOptions(request.Method) || HttpMethods.IsTrace(request.Method))
            return false;

        if (string.Equals(request.Headers["Sec-Fetch-Site"], "cross-site", StringComparison.OrdinalIgnoreCase))
            return false;

        var origin = request.Headers.Origin.ToString();

        return string.IsNullOrEmpty(origin) || IsSameOrigin(request, origin);
    }

    private static bool IsSameOrigin(HttpRequest request, string origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var originUri))
            return false;

        return string.Equals(originUri.Authority, request.Host.Value, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Ответ для случая, когда токена нет вовсе (нет cookie и нет заголовка).</summary>
    public static ProblemDetails MissingToken(HttpContext context) => new()
    {
        Status = StatusCodes.Status400BadRequest,
        Title = "Ошибка проверки антифоржери-токена",
        Detail = "Токен не передан: получите его через GET /api/v1/auth/csrf — cookie XSRF-TOKEN отправится с запросом сама.",
        Instance = context.Request.Path,
        Extensions = { ["code"] = ErrorCodes.AntiforgeryTokenInvalid },
    };
}
