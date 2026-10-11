namespace SlotBook.SharedKernel.Application;

/// <summary>
/// The identity and time of a new integration event, taken from <see cref="IClock"/> (AD-9, AD-14):
/// the id is a version 7 Guid made from the clock's time.
/// </summary>
public sealed record EventStamp(Guid EventId, DateTimeOffset OccurredAt)
{
    public static EventStamp Now(IClock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);

        var now = clock.UtcNow;
        return new EventStamp(Guid.CreateVersion7(now), now);
    }
}
