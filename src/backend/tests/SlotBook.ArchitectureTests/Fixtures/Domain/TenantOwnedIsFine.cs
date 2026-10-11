#pragma warning disable CA1812, CA1852 // fixture: never instantiated
using SlotBook.SharedKernel.Domain;

namespace SlotBook.Modules.Fixture.Domain;

/// <summary>Not a violation: tenant-owned.</summary>
internal sealed class TenantOwnedIsFine(Guid id) : TenantAggregateRoot(id);
