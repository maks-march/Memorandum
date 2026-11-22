using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Memorandum.Api.Infrastructure.Errors;

/// <summary>
/// Последний рубеж: необработанное исключение превращается в ProblemDetails.
/// Наружу — только traceId, текст исключения виден лишь в Development.
/// </summary>
internal static class ExceptionHandlingExtensions
{
    public static void UseApiExceptionHandler(this WebApplication app) =>
        app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
        {
            var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

            logger.LogError(error, "Необработанное исключение. traceId={TraceId} path={Path}", traceId, context.Request.Path);

            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Внутренняя ошибка сервера",
                Detail = app.Environment.IsDevelopment()
                    ? error?.ToString()
                    : "Обратитесь к администратору и сообщите traceId.",
                Instance = context.Request.Path,
            };
            problem.Extensions["traceId"] = traceId;

            await context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
        }));
}
