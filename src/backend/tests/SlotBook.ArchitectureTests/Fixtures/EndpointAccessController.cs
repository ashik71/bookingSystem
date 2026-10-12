#pragma warning disable CA1515, CA1822 // fixture: public so MVC discovers it; actions are never called
#pragma warning disable ASP0026 // the conflict is the point
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SlotBook.Api.Auth;

namespace SlotBook.ArchitectureTests.Fixtures;

/// <summary>Deliberate AD-12 violations: no scheme, two schemes, anonymous plus a scheme, staff without roles, an unknown scheme.</summary>
[ApiController]
[Route("fixture/access")]
public sealed class EndpointAccessController : ControllerBase
{
    [HttpGet("none")]
    public IActionResult NoneDeclared() => Ok();

    [HttpGet("authorize-without-scheme")]
    [Authorize]
    public IActionResult AuthorizeWithoutScheme() => Ok();

    [HttpGet("both")]
    [Authorize(AuthenticationSchemes = "patient,staff")]
    public IActionResult BothSchemes() => Ok();

    [HttpGet("anonymous-and-scheme")]
    [AllowAnonymous]
    [AuthorizePatient]
    public IActionResult AnonymousAndScheme() => Ok();

    [HttpGet("staff-without-roles")]
    [AuthorizeStaff]
    public IActionResult StaffWithoutRoles() => Ok();

    [HttpGet("unknown-scheme")]
    [Authorize(AuthenticationSchemes = "admin")]
    public IActionResult UnknownScheme() => Ok();

    [HttpGet("valid-patient")]
    [AuthorizePatient]
    public IActionResult ValidPatient() => Ok();

    [HttpGet("valid-staff")]
    [AuthorizeStaff("owner")]
    public IActionResult ValidStaff() => Ok();
}
