using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SlotBook.Api.Security;

internal static class AntiforgerySetup
{
    public const string HeaderName = "X-XSRF-TOKEN";
    public const string RequestTokenCookie = "XSRF-TOKEN";
    private const string SecretCookie = "__Host-sb_xsrf";

    /// <summary>Antiforgery validating Angular's <c>X-XSRF-TOKEN</c> header on every unsafe method (AD-12).</summary>
    public static IServiceCollection AddSlotBookAntiforgery(this IServiceCollection services)
    {
        services.AddAntiforgery(options =>
        {
            options.HeaderName = HeaderName;
            options.Cookie.Name = SecretCookie;
            options.Cookie.Path = "/";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });

        // AddControllers alone leaves the framework's antiforgery filters unregistered, and AutoValidateAntiforgeryTokenAttribute resolves one.
        services.AddMvcCore().AddViews();
        services.Configure<MvcOptions>(options =>
        {
            options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
            options.Filters.Add(new XsrfInvalidResultFilter());
        });
        return services;
    }

    /// <summary>
    /// Stores a fresh token pair: the secret in the HttpOnly cookie, the request token in the cookie Angular reads. Sign-in and sign-out call this to re-issue.
    /// </summary>
    public static void IssueToken(HttpContext context)
    {
        var tokens = context.RequestServices.GetRequiredService<IAntiforgery>().GetAndStoreTokens(context);
        context.Response.Cookies.Append(RequestTokenCookie, tokens.RequestToken!, new CookieOptions
        {
            Path = "/",
            Secure = true,
            SameSite = SameSiteMode.Strict,
            HttpOnly = false,
        });
    }
}
