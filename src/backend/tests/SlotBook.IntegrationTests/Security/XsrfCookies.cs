namespace SlotBook.IntegrationTests.Security;

internal static class XsrfCookies
{
    /// <summary>The value of one cookie of a response's <c>Set-Cookie</c> headers.</summary>
    public static string Value(HttpResponseMessage response, string name)
    {
        var prefix = name + "=";
        var cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith(prefix, StringComparison.Ordinal));
        return cookie[prefix.Length..cookie.IndexOf(';', StringComparison.Ordinal)];
    }

    public static string RequestToken(HttpResponseMessage response) => Value(response, "XSRF-TOKEN");

    /// <summary>A <c>Cookie</c> header carrying both cookies the token endpoint set.</summary>
    public static string CookieHeader(HttpResponseMessage response) =>
        $"__Host-sb_xsrf={Value(response, "__Host-sb_xsrf")}; XSRF-TOKEN={RequestToken(response)}";
}
