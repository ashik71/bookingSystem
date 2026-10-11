using SlotBook.SharedKernel.Domain;

namespace SlotBook.IntegrationTests.TestSupport.Guards;

/// <summary>A keyed guard document (AD-6) for the concurrency tests: a tenant-owned counter with a derived id.</summary>
internal sealed class GuardCounter : TenantAggregateRoot
{
    public GuardCounter(Guid id)
        : base(id)
    {
    }

    // The rehydration constructor the driver uses.
    private GuardCounter()
        : base(Guid.Empty)
    {
    }

    public int Count { get; private set; }

    public void Increment() => Count++;
}
