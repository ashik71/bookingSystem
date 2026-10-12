using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;

namespace SlotBook.Api.Security;

internal static class ForwardedHeadersSetup
{
    /// <summary>
    /// Trusts <c>X-Forwarded-Proto</c> only, so <c>Request.IsHttps</c> is true behind a TLS-terminating proxy
    /// (the antiforgery cookies are <c>Secure</c>). The proxy's address is not fixed, hence no known proxies.
    /// Client IP (<c>X-Forwarded-For</c>, one trusted hop) is AD-22, proven in story 10; deliberately not applied here.
    /// </summary>
    public static ForwardedHeadersOptions Options()
    {
        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedProto,
            ForwardLimit = 1,
        };
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
        return options;
    }
}
