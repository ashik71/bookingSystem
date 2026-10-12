namespace SlotBook.SharedKernel.Infrastructure.Persistence.Leases;

/// <summary>A lease this host holds. <see cref="ExpiresAt"/> moves forward with every successful renewal.</summary>
internal sealed class Lease(string name, string holder, DateTime expiresAt)
{
    private long _expiresAtTicks = expiresAt.Ticks;

    public string Name { get; } = name;

    public string Holder { get; } = holder;

    public DateTime ExpiresAt
    {
        get => new(Interlocked.Read(ref _expiresAtTicks), DateTimeKind.Utc);
        set => Interlocked.Exchange(ref _expiresAtTicks, value.Ticks);
    }
}
