namespace SlotBook.SharedKernel.Infrastructure.Jobs;

/// <summary>
/// A job the runner starts every <see cref="Period"/>, on one host at a time (AD-11). A run must be idempotent and
/// catch up: it processes everything due since its stored state, not only "this tick". There is no fencing token, so
/// a host that paused past its lease expiry can still write before it notices; the job must tolerate that.
/// </summary>
public interface ITimedJob
{
    /// <summary>Names the job and its lease (<c>job:{Name}</c>).</summary>
    public string Name { get; }

    public TimeSpan Period { get; }

    /// <summary>Runs once. <paramref name="cancellationToken"/> is cancelled when the lease is lost or the host stops.</summary>
    public Task RunAsync(CancellationToken cancellationToken);
}
