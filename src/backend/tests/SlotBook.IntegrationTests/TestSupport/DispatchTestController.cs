#pragma warning disable CA1822 // MVC actions are instance methods
#pragma warning disable CA1515 // public: discovered by MVC
using Microsoft.AspNetCore.Mvc;
using SlotBook.Api.Errors;
using SlotBook.SharedKernel.Application.Dispatching;

namespace SlotBook.IntegrationTests.TestSupport;

[ApiController]
[Route("test-only/probes")]
public class DispatchTestController(IDispatcher dispatcher) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateAsync(CreateProbeCommand command, CancellationToken cancellationToken) =>
        (await dispatcher.SendAsync(command, cancellationToken)).ToActionResult(this, Ok);

    [HttpPost("refuse")]
    public async Task<IActionResult> RefuseAsync(RefuseProbeCommand command, CancellationToken cancellationToken) =>
        (await dispatcher.SendAsync(command, cancellationToken)).ToActionResult(this, Ok);

    [HttpGet("throw-domain/{code}")]
    public IActionResult ThrowDomain(string code) => throw new SlotBook.SharedKernel.Domain.DomainException(code);
}
