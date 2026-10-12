using Microsoft.AspNetCore.Authentication;

namespace SlotBook.Api.Auth;

internal sealed class SessionAuthenticationOptions : AuthenticationSchemeOptions
{
    public string CookieName { get; set; } = string.Empty;
}
