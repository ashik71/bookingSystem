using Microsoft.AspNetCore.Authentication;

namespace SlotBook.Api.Auth;

internal static class AuthSchemes
{
    public const string Patient = "patient";
    public const string Staff = "staff";
    public const string PatientCookie = "__Host-sb_patient";
    public const string StaffCookie = "__Host-sb_staff";

    /// <summary>Registers the two session schemes (AD-12). There is deliberately no default scheme: every endpoint names its own.</summary>
    public static IServiceCollection AddSlotBookAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication()
            .AddScheme<SessionAuthenticationOptions, SessionAuthenticationHandler>(Patient, options => options.CookieName = PatientCookie)
            .AddScheme<SessionAuthenticationOptions, SessionAuthenticationHandler>(Staff, options => options.CookieName = StaffCookie);
        services.AddAuthorization();
        return services;
    }
}
