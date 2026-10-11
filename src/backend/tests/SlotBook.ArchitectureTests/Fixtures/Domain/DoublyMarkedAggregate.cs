#pragma warning disable CA1812, CA1852 // fixture: never instantiated
using SlotBook.SharedKernel.Domain;

namespace SlotBook.Modules.Fixture.Domain;

/// <summary>Deliberate AD-5 violation: tenant-owned and global at once.</summary>
[GlobalAggregate("tenants")]
internal sealed class DoublyMarkedAggregate(Guid id) : TenantAggregateRoot(id);
