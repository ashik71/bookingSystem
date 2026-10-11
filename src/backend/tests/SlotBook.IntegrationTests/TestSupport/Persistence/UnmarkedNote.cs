#pragma warning disable CA1812 // resolved from the container or used as a type argument
using SlotBook.SharedKernel.Domain;

namespace SlotBook.IntegrationTests.TestSupport.Persistence;

/// <summary>Neither tenant-owned nor global: the collection must refuse it (AD-5).</summary>
internal sealed class UnmarkedNote(Guid id) : AggregateRoot(id);
