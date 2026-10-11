#pragma warning disable CA1812, CA1852 // fixture: never instantiated
using SlotBook.SharedKernel.Domain;

namespace SlotBook.Modules.Fixture.Domain;

/// <summary>Deliberate AD-5 violation: an aggregate that is neither tenant-owned nor global.</summary>
internal sealed class UnmarkedAggregate(Guid id) : AggregateRoot(id);
