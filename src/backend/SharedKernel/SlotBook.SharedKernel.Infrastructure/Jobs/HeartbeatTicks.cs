namespace SlotBook.SharedKernel.Infrastructure.Jobs;

/// <summary>Tick arithmetic of the heartbeat: ticks are the multiples of the period since the Unix epoch.</summary>
internal static class HeartbeatTicks
{
    /// <summary>
    /// The ticks to record in <c>(lastTickAt, now]</c>, oldest first, at most <paramref name="maxTicks"/> (the newest ones;
    /// <c>Skipped</c> counts the older ones dropped). With no <paramref name="lastTickAt"/> (the first run ever) it is only
    /// the current tick.
    /// </summary>
    public static (IReadOnlyList<DateTime> Due, int Skipped) Due(DateTime? lastTickAt, DateTime now, TimeSpan period, int maxTicks)
    {
        var current = Index(now, period);
        var first = lastTickAt is null ? current : Index(lastTickAt.Value, period) + 1;
        if (first > current)
        {
            return ([], 0);
        }

        var count = current - first + 1;
        var skipped = (int)Math.Max(0, count - maxTicks);
        first += skipped;
        var due = new List<DateTime>((int)(current - first + 1));
        for (var index = first; index <= current; index++)
        {
            due.Add(DateTime.UnixEpoch.AddTicks(index * period.Ticks));
        }

        return (due, skipped);
    }

    private static long Index(DateTime moment, TimeSpan period) => (moment.ToUniversalTime() - DateTime.UnixEpoch).Ticks / period.Ticks;
}
