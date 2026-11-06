using Microsoft.AspNetCore.Mvc;

namespace Memorandum.Api;

public static class ResultMapping
{
    public static ActionResult<T> ToActionResult<T>(this Fin<T> fin) =>
        fin.Match<ActionResult<T>>(
            Succ: value => new OkObjectResult(value),
            Fail: error => new ObjectResult(ToProblem(error))
        );

    
    public static ActionResult<TResult> ToActionResult<T, TResult>(this Fin<T> fin, Func<T, TResult> map) =>
        fin.Match<ActionResult<TResult>>(
            Succ: value => new OkObjectResult(map(value)),
            Fail: error => new ObjectResult(ToProblem(error))
        );

    public static IActionResult ToNoContentResult(this Fin<Unit> fin) =>
        fin.Match<IActionResult>(
            Succ: value => new NoContentResult(),
            Fail: error => new ObjectResult(ToProblem(error))
        );

    private static ProblemDetails ToProblem(Error error) => new ProblemDetails()
    {
        Status = error.Code,
        Title = error.Message,
        Extensions = { ["traceId"] = System.Diagnostics.Activity.Current?.Id }
    };
}