#pragma warning disable CA1822 // MVC actions are instance methods
#pragma warning disable CA1515 // public: used as an xunit fixture type, or discovered by MVC
using Microsoft.AspNetCore.Mvc;
using SlotBook.SharedKernel.Domain;

namespace SlotBook.IntegrationTests.TestSupport;

[ApiController]
[Route("test-only")]
public class FailingTestController : ControllerBase
{
    [HttpGet("throw")]
    public IActionResult Throw() => throw new InvalidOperationException("secret internal detail");

    [HttpGet("refuse")]
    public IActionResult Refuse() => throw new DomainException("slot_full");

    [HttpPost("post-only")]
    public IActionResult PostOnly() => Ok();
}
