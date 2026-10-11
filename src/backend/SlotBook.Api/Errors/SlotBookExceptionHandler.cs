using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SlotBook.SharedKernel.Application;
using SlotBook.SharedKernel.Domain;

namespace SlotBook.Api.Errors;

#pragma warning disable CA1812 // instantiated by dependency injection

internal sealed partial class SlotBookExceptionHandler(
    IProblemDetailsService problemDetails,
    IHostEnvironment environment,
    ILogger<SlotBookExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var problem = new ProblemDetails();
        if (exception is DomainException domain)
        {
            problem.Status = StatusCodes.Status422UnprocessableEntity;
            problem.Extensions["code"] = domain.Code;
        }
        else
        {
            LogUnhandledException(exception);
            problem.Status = StatusCodes.Status500InternalServerError;
            problem.Extensions["code"] = CommonErrors.InternalError;
            if (environment.IsDevelopment())
            {
                problem.Detail = exception.ToString();
            }
        }

        httpContext.Response.StatusCode = problem.Status.Value;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Unhandled exception")]
    private partial void LogUnhandledException(Exception exception);
}
