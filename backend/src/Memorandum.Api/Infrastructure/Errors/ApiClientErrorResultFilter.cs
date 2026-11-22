using System.Diagnostics;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Memorandum.Domain.Common;

namespace Memorandum.Api.Infrastructure.Errors;

/// <summary>
/// Провал антифоржери приходит как «клиентская ошибка 400» без подробностей:
/// ASP.NET отдаёт title «Bad Request» и звёздочку в type, а фронту нужно знать,
/// перевыпустить токен или отправить его вообще. Подменяем тело на ProblemDetails
/// с кодом и причиной; сама проверка токена остаётся штатной
/// (<c>[RequireAntiforgeryToken]</c> + <c>UseAntiforgery</c>).
/// </summary>
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

        var detail = (noHeader, noCookie) switch
        {
            (true, _) => "Токен не передан: получите его через GET /api/v1/auth/csrf и отправьте в заголовке X-XSRF-TOKEN.",
            (_, true) => "Cookie XSRF-TOKEN отсутствует или истекла: перевыпустите токен через GET /api/v1/auth/csrf.",
            _ => "Токен не совпадает с cookie XSRF-TOKEN: перевыпустите его через GET /api/v1/auth/csrf.",
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
