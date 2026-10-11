#pragma warning disable CA1812 // instantiated by the dispatcher scan
using SlotBook.SharedKernel.Application;
using SlotBook.SharedKernel.Application.Dispatching;
using SlotBook.SharedKernel.Domain;

namespace SlotBook.IntegrationTests.TestSupport.Guards;

/// <summary>Get-or-create the keyed guard (AD-6), increment it, commit. Two first writers collide on one <c>_id</c> and one re-runs.</summary>
internal sealed class IncrementCounterCommandHandler(GuardCounterWriteRepository counters, IUnitOfWork unitOfWork, ITenantContext tenant)
    : ICommandHandler<IncrementCounterCommand, int>
{
    public async Task<Result<int>> HandleAsync(IncrementCounterCommand command, CancellationToken cancellationToken)
    {
        var id = DeterministicId.ForGuard(tenant.TenantId, "counter", command.Key);
        var counter = await counters.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (counter is null)
        {
            counter = new GuardCounter(id);
            counters.Add(counter);
        }

        counter.Increment();
        await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
        return counter.Count;
    }
}
