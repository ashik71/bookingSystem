using Microsoft.AspNetCore.Mvc;
using SlotBook.SharedKernel.Application;

namespace SlotBook.Api.Errors;

/// <summary>Turns a <see cref="Result{T}"/> into an HTTP response (AD-13).</summary>
internal static class ResultExtensions
{
    /// <summary>Success → <paramref name="onSuccess"/>; refusal → problem with its code (404 for <c>not_found</c>, 409 for <c>concurrency_conflict</c>, else 422).</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="controller">The calling controller; supplies the problem factory.</param>
    /// <param name="onSuccess">Builds the success response.</param>
    /// <returns>The action result.</returns>
    public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller, Func<T, IActionResult> onSuccess)
    {
        ArgumentNullException.ThrowIfNull(controller);
        ArgumentNullException.ThrowIfNull(onSuccess);
        return result.IsRefused
            ? Refusal(controller, result.Code!)
            : onSuccess(result.Value);
    }

    internal static ObjectResult Refusal(ControllerBase controller, string code)
    {
        var status = StatusFor(code);
        var problem = controller.ProblemDetailsFactory.CreateProblemDetails(controller.HttpContext, status);
        problem.Extensions["code"] = code;
        return new ObjectResult(problem) { StatusCode = status };
    }

    internal static int StatusFor(string code) => code switch
    {
        CommonErrors.NotFound => StatusCodes.Status404NotFound,
        CommonErrors.ConcurrencyConflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status422UnprocessableEntity,
    };
}
