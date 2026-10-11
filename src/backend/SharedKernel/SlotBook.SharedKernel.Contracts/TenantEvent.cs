namespace SlotBook.SharedKernel.Contracts;

/// <summary>
/// The base of every integration event (AD-2, AD-10). A concrete event is a sealed record that carries
/// ids and facts, never an aggregate, and is published as its own type. <see cref="EventId"/> is also
/// the message id on the bus, so the inbox can tell a redelivery from a new event.
/// </summary>
public abstract record TenantEvent
{
    public required Guid EventId { get; init; }

    public required Guid TenantId { get; init; }

    public required DateTimeOffset OccurredAt { get; init; }
}
