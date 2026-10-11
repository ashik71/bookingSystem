#pragma warning disable CA1812, CA1852 // fixture: never instantiated
using SlotBook.SharedKernel.Domain;

namespace SlotBook.Modules.Fixture.Domain;

/// <summary>Not a violation: global.</summary>
[GlobalAggregate("leases")]
internal sealed class GlobalIsFine(Guid id) : AggregateRoot(id);
