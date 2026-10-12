using SlotBook.SharedKernel.Domain;

namespace SlotBook.IntegrationTests.TestSupport.Messaging;

/// <summary>What the consumer's command writes: a tenant-owned counter with a derived id, so "ran once" can be read from the database.</summary>
internal sealed class NoteSeen : TenantAggregateRoot
{
    public NoteSeen(Guid id)
        : base(id)
    {
    }

    // The rehydration constructor the driver uses.
    private NoteSeen()
        : base(Guid.Empty)
    {
    }

    public int Count { get; private set; }

    public void Increment() => Count++;
}
