namespace SlotBook.SharedKernel.Infrastructure.Persistence.Leases;

/// <summary>A lease one acquisition holds. <see cref="ExpiresAt"/> moves forward with every successful renewal.</summary>
internal sealed class Lease(string name, string holder, string token, DateTime expiresAt)
{
    private long _expiresAtTicks = expiresAt.Ticks;

    public string Name { get; } = name;

    public string Holder { get; } = holder;

    /// <summary>Random per acquisition; renew and release match on it.</summary>
    public string Token { get; } = token;

    public DateTime ExpiresAt
    {
        get => new(Interlocked.Read(ref _expiresAtTicks), DateTimeKind.Utc);
        set => Interlocked.Exchange(ref _expiresAtTicks, value.Ticks);
    }
}
