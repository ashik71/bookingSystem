#pragma warning disable CA1822 // MVC actions are instance methods
#pragma warning disable CA1515 // public: discovered by MVC
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SlotBook.Api.Auth;

namespace SlotBook.IntegrationTests.TestSupport;

/// <summary>Probes for antiforgery, forwarded headers and the two authentication schemes.</summary>
[ApiController]
[Route("test-only/security")]
public class SecurityProbeController : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("protected")]
    public IActionResult Protected() => Ok();

    [AllowAnonymous]
    [HttpGet("remote-ip")]
    public IActionResult RemoteIp() => Ok(HttpContext.Connection.RemoteIpAddress?.ToString());

    [AllowAnonymous]
    [HttpGet("open")]
    public IActionResult Open() => Ok();

    [HttpGet("patient")]
    [AuthorizePatient]
    public IActionResult PatientOnly() => Ok();

    [HttpGet("staff")]
    [AuthorizeStaff("owner")]
    public IActionResult StaffOnly() => Ok();
}
