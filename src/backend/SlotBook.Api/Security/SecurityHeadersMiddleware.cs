using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace SlotBook.Api.Security;

#pragma warning disable CA1812 // instantiated by UseMiddleware

/// <summary>
/// AD-18: every API response carries <c>Content-Security-Policy: default-src 'none'</c> (the API serves
/// no documents) and, outside Development, HSTS. The headers are added when the response starts, so
/// the exception handler, which clears the response, cannot drop them.
/// </summary>
internal sealed class SecurityHeadersMiddleware(RequestDelegate next, IHostEnvironment environment)
{
    internal const string ContentSecurityPolicy = "default-src 'none'";
    internal const string StrictTransportSecurity = "max-age=31536000";

    private readonly bool _sendHsts = !environment.IsDevelopment();

    public Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        context.Response.OnStarting(
            static state =>
            {
                var (response, sendHsts) = ((HttpResponse, bool))state;
                response.Headers.ContentSecurityPolicy = ContentSecurityPolicy;
                if (sendHsts)
                {
                    response.Headers.StrictTransportSecurity = StrictTransportSecurity;
                }

                return Task.CompletedTask;
            },
            (context.Response, _sendHsts));
        return next(context);
    }
}
