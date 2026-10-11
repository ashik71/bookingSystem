#pragma warning disable CA1515, CA1822 // fixture: public so MVC discovers it; actions are never called
using Microsoft.AspNetCore.Mvc;

namespace SlotBook.ArchitectureTests.Fixtures;

/// <summary>Deliberate AD-5 violations: a tenant in the route, the query string and a header.</summary>
[ApiController]
public sealed class TenantParameterController : ControllerBase
{
    [HttpGet("fixture/{tenantId}/route")]
    public IActionResult InRoute(string tenantId) => Ok(tenantId);

    [HttpGet("fixture/query")]
    public IActionResult InQuery([FromQuery] string tenant) => Ok(tenant);

    [HttpGet("fixture/header")]
    public IActionResult InHeader([FromHeader(Name = "X-Tenant")] string value) => Ok(value);
}
