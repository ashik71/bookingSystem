#pragma warning disable CA1812, CA1852 // fixture: never instantiated
using SlotBook.SharedKernel.Domain;

namespace SlotBook.Modules.Fixture.Domain;

/// <summary>Not a violation: an abstract base is checked through its concrete aggregates.</summary>
internal abstract class AbstractBaseIsFine(Guid id) : AggregateRoot(id);
