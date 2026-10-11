using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SlotBook.ServiceDefaults;

#pragma warning disable CA1812 // instantiated by UseMiddleware

internal sealed partial class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
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
            LogRequest(context.Request.Method, context.Request.Path.Value ?? "/", context.Response.StatusCode);
        }
    }

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "{Method} {Path} responded {StatusCode}")]
    private partial void LogRequest(string method, string path, int statusCode);
}
