using Microsoft.AspNetCore.Authorization;

namespace SlotBook.Api.Auth;

/// <summary>The endpoint needs a staff session holding one of <c>roles</c>.</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
internal sealed class AuthorizeStaffAttribute : AuthorizeAttribute
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1019", Justification = "Roles is the inherited string property")]
    public AuthorizeStaffAttribute(params string[] roles)
    {
        AuthenticationSchemes = AuthSchemes.Staff;
        Roles = string.Join(',', roles);
    }
}
