namespace SlotBook.SharedKernel.Domain;

/// <summary>
/// Base of an aggregate that belongs to the tenant (AD-5). Its repository filters every read and
/// write by <c>tenantId</c> and stamps it on insert; the domain never sees it.
/// </summary>
public abstract class TenantAggregateRoot : AggregateRoot
{
    protected TenantAggregateRoot(Guid id)
        : base(id)
    {
    }
}
