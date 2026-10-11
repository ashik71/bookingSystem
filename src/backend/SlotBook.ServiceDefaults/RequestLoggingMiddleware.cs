using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SlotBook.ServiceDefaults;

#pragma warning disable CA1812 // instantiated by UseMiddleware

internal sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            await next(context);
        }
        finally
        {
            logger.LogRequest(context.Request.Method, context.Request.Path.Value ?? "/", context.Response.StatusCode);
        }
    }
}
