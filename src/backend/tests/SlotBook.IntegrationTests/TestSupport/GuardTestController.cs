#pragma warning disable CA1822 // MVC actions are instance methods
#pragma warning disable CA1515 // public: discovered by MVC
using Microsoft.AspNetCore.Mvc;
using SlotBook.Api.Errors;
using SlotBook.IntegrationTests.TestSupport.Guards;
using SlotBook.SharedKernel.Application.Dispatching;

namespace SlotBook.IntegrationTests.TestSupport;

[ApiController]
[Route("test-only/counters")]
public class GuardTestController(IDispatcher dispatcher) : ControllerBase
{
    [HttpPost("{key}/increment")]
    public async Task<IActionResult> IncrementAsync(string key, CancellationToken cancellationToken) =>
        (await dispatcher.SendAsync(new IncrementCounterCommand(key), cancellationToken)).ToActionResult(this, count => Ok(count));

    [HttpPost("{key}/conflict")]
    public async Task<IActionResult> ConflictAsync(string key, CancellationToken cancellationToken) =>
        (await dispatcher.SendAsync(new ConflictCommand(key, RefuseOnRerun: false), cancellationToken)).ToActionResult(this, count => Ok(count));

    [HttpPost("{key}/conflict-then-refuse")]
    public async Task<IActionResult> ConflictThenRefuseAsync(string key, CancellationToken cancellationToken) =>
        (await dispatcher.SendAsync(new ConflictCommand(key, RefuseOnRerun: true), cancellationToken)).ToActionResult(this, count => Ok(count));
}
