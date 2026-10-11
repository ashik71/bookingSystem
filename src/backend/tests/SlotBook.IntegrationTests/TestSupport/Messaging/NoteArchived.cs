#pragma warning disable CA1515 // public: serialized and registered by MassTransit
using SlotBook.SharedKernel.Contracts;

namespace SlotBook.IntegrationTests.TestSupport.Messaging;

/// <summary>A second test event, so the round-trip test covers more than one type and a collection property.</summary>
public sealed record NoteArchived : TenantEvent
{
    public required Guid NoteId { get; init; }

    public required IReadOnlyList<string> Tags { get; init; }

    public required string Kind { get; init; }
}
