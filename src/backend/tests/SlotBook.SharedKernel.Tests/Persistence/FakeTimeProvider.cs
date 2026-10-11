namespace SlotBook.SharedKernel.Tests.Persistence;

/// <summary>A clock that moves only when a delay is awaited: timers fire at once and advance time by their due time.</summary>
internal sealed class FakeTimeProvider : TimeProvider
{
    private long _ticks;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public TimeSpan Elapsed => TimeSpan.FromTicks(_ticks);

    public int Delays { get; private set; }

    public override long GetTimestamp() => _ticks;

    public void Advance(TimeSpan by) => _ticks += by.Ticks;

    public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
    {
        Delays++;
        Advance(dueTime);
        callback(state);
        return new NoTimer();
    }

    private sealed class NoTimer : ITimer
    {
        public bool Change(TimeSpan dueTime, TimeSpan period) => true;

        public void Dispose()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
