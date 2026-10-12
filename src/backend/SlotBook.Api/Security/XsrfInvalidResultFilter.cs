using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SlotBook.SharedKernel.Application;

namespace SlotBook.Api.Security;

/// <summary>Rewrites MVC's empty antiforgery refusal as 400 problem details with code <c>xsrf_invalid</c> (AD-13).</summary>
internal sealed class XsrfInvalidResultFilter : IAlwaysRunResultFilter, IOrderedFilter
{
    /// <summary>Before the <c>[ApiController]</c> client-error filter (-2000), which would turn the refusal into a generic 400 first.</summary>
    public int Order => -3000;

    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is not Microsoft.AspNetCore.Mvc.Core.Infrastructure.IAntiforgeryValidationFailedResult)
        {
            return;
        }

        var problem = new ProblemDetails { Status = StatusCodes.Status400BadRequest };
        problem.Extensions["code"] = CommonErrors.XsrfInvalid;
        context.Result = new ObjectResult(problem) { StatusCode = StatusCodes.Status400BadRequest };
    }

    public void OnResultExecuted(ResultExecutedContext context)
    {
    }
}
