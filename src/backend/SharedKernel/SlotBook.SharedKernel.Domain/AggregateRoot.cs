namespace SlotBook.SharedKernel.Domain;

/// <summary>
/// Base of every aggregate. It holds the id and nothing else: <c>version</c>, <c>tenantId</c>,
/// <c>createdAt</c> and <c>updatedAt</c> belong to the stored document, which the unit of work
/// maintains (AD-7, AD-9). Derive from <see cref="TenantAggregateRoot"/> or mark the type
/// <see cref="GlobalAggregateAttribute"/> (AD-5).
/// </summary>
public abstract class AggregateRoot
{
    protected AggregateRoot(Guid id)
    {
        Id = id;
    }

    /// <summary>Gets the id, stored as <c>_id</c>.</summary>
    public Guid Id { get; private set; }
}
