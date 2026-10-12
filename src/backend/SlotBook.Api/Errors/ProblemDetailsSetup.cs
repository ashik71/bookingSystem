using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using SlotBook.SharedKernel.Application;

namespace SlotBook.Api.Errors;

internal static class ProblemDetailsSetup
{
    /// <summary>Gives every problem body the W3C trace id (AD-17) and a stable <c>code</c> (AD-13).</summary>
    public static void Customize(ProblemDetailsContext context)
    {
        var problem = context.ProblemDetails;
        problem.Extensions["traceId"] = Activity.Current?.TraceId.ToHexString() ?? context.HttpContext.TraceIdentifier;

        if (!problem.Extensions.ContainsKey("code") && CodeFor(problem.Status) is { } code)
        {
            problem.Extensions["code"] = code;
        }
    }

    /// <summary>Re-writes 404 and 4xx framework statuses as problem details (405 and 415 become 400).</summary>
    public static async Task WriteStatusCodeAsync(StatusCodeContext context)
    {
        var http = context.HttpContext;
        var status = http.Response.StatusCode;
        if (status is StatusCodes.Status405MethodNotAllowed or StatusCodes.Status415UnsupportedMediaType)
        {
            status = StatusCodes.Status400BadRequest;
            http.Response.StatusCode = status;
        }

        var service = http.RequestServices.GetRequiredService<IProblemDetailsService>();
        await service.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            ProblemDetails = { Status = status },
        });
    }

    private static string? CodeFor(int? status) => status switch
    {
        StatusCodes.Status400BadRequest => CommonErrors.ValidationFailed,
        StatusCodes.Status401Unauthorized => CommonErrors.Unauthenticated,
        StatusCodes.Status403Forbidden => CommonErrors.Forbidden,
        StatusCodes.Status404NotFound => CommonErrors.NotFound,
        StatusCodes.Status409Conflict => CommonErrors.ConcurrencyConflict,
        StatusCodes.Status429TooManyRequests => CommonErrors.RateLimited,
        StatusCodes.Status500InternalServerError => CommonErrors.InternalError,
        _ => null,
    };
}
