namespace SlotBook.SharedKernel.Application;

/// <summary>The tenant this deployment serves (AD-5).</summary>
public interface ITenantContext
{
    public Guid TenantId { get; }
}
