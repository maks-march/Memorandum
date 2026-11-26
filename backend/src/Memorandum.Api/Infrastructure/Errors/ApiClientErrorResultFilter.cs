using System.Diagnostics;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Memorandum.Domain.Common;

namespace Memorandum.Api.Infrastructure.Errors;

/// <summary>Понятное тело ошибки антифоржери.</summary>
/// <remarks>
/// ASP.NET отдаёт на провал токена «клиентскую ошибку 400» без подробностей, а фронту
/// нужно знать, перевыпустить токен или отправить его вообще. Тело подменяется на
/// <c>ProblemDetails</c> с кодом и причиной; сама проверка остаётся штатной
/// (<c>[RequireAntiforgeryToken]</c> + <c>UseAntiforgery</c>).
/// </remarks>
internal sealed class ApiClientErrorResultFilter : IAlwaysRunResultFilter, IOrderedFilter
{
    /// <summary>Штатный фильтр клиентских ошибок идёт с порядком −2000.</summary>
    public int Order => -2000;

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }

    public void OnResultExecuting(ResultExecutingContext context)
    {
        // Ошибку антифоржери приносит фильтр авторизации: он прерывает конвейер,
        // поэтому нужен «всегда выполняемый» фильтр, а не обычный result-фильтр.
        // Остальные клиентские ошибки не трогаем: для них есть штатный
        // ClientErrorResultFilter с тем же порядком, и он идёт следом.
        if (context.HttpContext.Features.Get<IAntiforgeryValidationFeature>() is not { IsValid: false })
            return;

        context.Result = new ObjectResult(Problem(context.HttpContext)) { StatusCode = StatusCodes.Status400BadRequest };
    }

    /// <summary>Разные причины — разные подсказки: «не прислали», «протухла cookie», «не совпало».</summary>
    private static ProblemDetails Problem(HttpContext httpContext)
    {
        var noHeader = string.IsNullOrEmpty(httpContext.Request.Headers["X-XSRF-TOKEN"]);
        var noCookie = !httpContext.Request.Cookies.ContainsKey("XSRF-TOKEN");

        // Заголовок присылает клиент — сервер его не подставляет. Поэтому первым
        // делом проверяем наличие заголовка, а не cookie.
        var detail = (noHeader, noCookie) switch
        {
            (true, _) => "Заголовок X-XSRF-TOKEN не передан: получите токен через GET /api/v1/auth/csrf.",
            (false, true) => "Cookie XSRF-TOKEN отсутствует или истекла: перевыпустите токен через GET /api/v1/auth/csrf.",
            _ => "Заголовок X-XSRF-TOKEN не совпадает с cookie XSRF-TOKEN: перевыпустите токен через GET /api/v1/auth/csrf.",
        };

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Ошибка проверки антифоржери-токена",
            Detail = detail,
            Instance = httpContext.Request.Path,
        };
        problem.Extensions["code"] = ErrorCodes.AntiforgeryTokenInvalid;
        problem.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        return problem;
    }
}
