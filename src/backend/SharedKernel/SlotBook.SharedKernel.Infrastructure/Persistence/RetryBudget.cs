namespace SlotBook.SharedKernel.Infrastructure.Persistence;

/// <summary>
/// The time budget of one command (AD-7): 3 s counted from the first attempt, not an attempt count.
/// Backoff is exponential with full jitter, about 5 ms doubling to a 100 ms cap.
/// </summary>
internal sealed class RetryBudget
{
    internal static readonly TimeSpan DefaultTotal = TimeSpan.FromSeconds(3);

    private static readonly TimeSpan _firstBackoff = TimeSpan.FromMilliseconds(5);
    private static readonly TimeSpan _maxBackoff = TimeSpan.FromMilliseconds(100);

    private readonly TimeProvider _time;
    private readonly long _start;

    public RetryBudget(TimeProvider time, TimeSpan total)
    {
        ArgumentNullException.ThrowIfNull(time);
        _time = time;
        Total = total;
        _start = time.GetTimestamp();
    }

    public TimeSpan Total { get; }

    public TimeSpan Remaining => Total - _time.GetElapsedTime(_start);

    public bool IsExhausted => Remaining <= TimeSpan.Zero;

    /// <summary>Waits a jittered backoff before retry number <paramref name="retry"/>, never longer than what is left.</summary>
    public async Task BackoffAsync(int retry, CancellationToken cancellationToken)
    {
#pragma warning disable CA5394 // jitter, not security
        var jitter = TimeSpan.FromTicks((long)(Random.Shared.NextDouble() * CeilingFor(retry).Ticks));
#pragma warning restore CA5394
        var delay = TimeSpan.FromMilliseconds(Math.Max(1, jitter.TotalMilliseconds));
        var remaining = Remaining;
        if (remaining <= TimeSpan.Zero)
        {
            return;
        }

        await Task.Delay(delay < remaining ? delay : remaining, _time, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>The upper bound of the backoff before retry number <paramref name="retry"/> (1-based).</summary>
    internal static TimeSpan CeilingFor(int retry)
    {
        var ceiling = _firstBackoff;
        for (var i = 1; i < retry && ceiling < _maxBackoff; i++)
        {
            ceiling *= 2;
        }

        return ceiling < _maxBackoff ? ceiling : _maxBackoff;
    }
}
