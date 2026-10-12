#pragma warning disable CA1822 // MVC actions are instance methods
#pragma warning disable CA1515 // public: discovered by MVC
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SlotBook.IntegrationTests.TestSupport;

/// <summary>A POST that requires antiforgery, as every production unsafe endpoint does.</summary>
[ApiController]
[AllowAnonymous]
[Route("test-only/security")]
public class SecurityProbeController : ControllerBase
{
    [HttpPost("protected")]
    public IActionResult Protected() => Ok();

    [HttpGet("open")]
    public IActionResult Open() => Ok();
}
