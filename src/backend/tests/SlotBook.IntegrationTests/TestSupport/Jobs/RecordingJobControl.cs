#pragma warning disable CA1515 // public: used as a service of test hosts
using System.Collections.Concurrent;

namespace SlotBook.IntegrationTests.TestSupport.Jobs;

/// <summary>Shared by the hosts of a test: what the <see cref="RecordingJob"/> does, and what it recorded.</summary>
public sealed class RecordingJobControl
{
    private int _started;
    private int _failuresLeft;
    private int _failFirstRuns;

    public string Name { get; init; } = "recording";

    public TimeSpan Period { get; init; } = TimeSpan.FromMilliseconds(200);

    public TimeSpan RunDuration { get; init; } = TimeSpan.FromMilliseconds(300);

    /// <summary>The first this many runs throw.</summary>
    public int FailFirstRuns
    {
        get => _failFirstRuns;
        init
        {
            _failFirstRuns = value;
            _failuresLeft = value;
        }
    }

    public ConcurrentQueue<RecordedRun> Runs { get; } = new();

    /// <summary>Holders whose run was cancelled by the runner.</summary>
    public ConcurrentQueue<string> CancelledHolders { get; } = new();

    public int Started => Volatile.Read(ref _started);

    internal int NextRun() => Interlocked.Increment(ref _started);

    internal bool ShouldFail() => Interlocked.Decrement(ref _failuresLeft) >= 0;
}
