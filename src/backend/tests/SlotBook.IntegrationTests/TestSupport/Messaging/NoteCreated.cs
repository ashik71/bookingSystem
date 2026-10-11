#pragma warning disable CA1515 // public: serialized and registered by MassTransit
using SlotBook.SharedKernel.Contracts;

namespace SlotBook.IntegrationTests.TestSupport.Messaging;

/// <summary>A test integration event: ids and facts only.</summary>
public sealed record NoteCreated : TenantEvent
{
    public required Guid NoteId { get; init; }

    public required string Text { get; init; }
}
