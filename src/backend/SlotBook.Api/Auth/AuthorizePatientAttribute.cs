using Microsoft.AspNetCore.Authorization;

namespace SlotBook.Api.Auth;

/// <summary>The endpoint needs a patient session. With <see cref="AuthorizeStaffAttribute"/>, the only way to declare a scheme (AD-12).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
internal sealed class AuthorizePatientAttribute : AuthorizeAttribute
{
    public AuthorizePatientAttribute() => AuthenticationSchemes = AuthSchemes.Patient;
}
