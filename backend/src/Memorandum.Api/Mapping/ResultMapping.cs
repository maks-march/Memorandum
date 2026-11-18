using Microsoft.AspNetCore.Mvc;
using Memorandum.Application.Common;
using Memorandum.Domain.Common;

namespace Memorandum.Api.Mapping;

/// <summary>
/// Единственное место, где код ошибки Application превращается в HTTP-статус.
/// Тело — ProblemDetails (application/problem+json), коды стабильны для фронта.
/// </summary>
internal static class ResultMapping
{
    public static IActionResult ToProblem(this Error error, ControllerBase controller)
    {
        var traceId = controller.HttpContext.TraceIdentifier;

        if (error.FieldErrors is { Count: > 0 })
        {
            var validation = new HttpValidationProblemDetails(
                new Dictionary<string, string[]>(error.FieldErrors))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Ошибка валидации",
                Detail = error.Message,
            };
            validation.Extensions["traceId"] = traceId;

            return new BadRequestObjectResult(validation);
        }

        var status = StatusFor(error.Code);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = TitleFor(status),
            Detail = error.Message,
            Instance = controller.HttpContext.Request.Path,
        };
        problem.Extensions["code"] = error.Code;
        problem.Extensions["traceId"] = traceId;

        if (error.Details is { Count: > 0 })
            problem.Extensions["details"] = error.Details;

        return new ObjectResult(problem) { StatusCode = status };
    }

    private static int StatusFor(string code) => code switch
    {
        ErrorCodes.NotFound => StatusCodes.Status404NotFound,
        ErrorCodes.Forbidden => StatusCodes.Status403Forbidden,
        ErrorCodes.InvalidCredentials or ErrorCodes.UserInactive => StatusCodes.Status401Unauthorized,
        ErrorCodes.EmailDuplicate or ErrorCodes.ReferenceInUse
            or ErrorCodes.MemberAlreadyExists or ErrorCodes.OrganizationInactive
            or ErrorCodes.LastOrgAdminRequired => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status400BadRequest,
    };

    private static string TitleFor(int status) => status switch
    {
        StatusCodes.Status401Unauthorized => "Требуется вход",
        StatusCodes.Status403Forbidden => "Недостаточно прав",
        StatusCodes.Status404NotFound => "Не найдено",
        StatusCodes.Status409Conflict => "Конфликт состояния",
        _ => "Некорректный запрос",
    };
}
