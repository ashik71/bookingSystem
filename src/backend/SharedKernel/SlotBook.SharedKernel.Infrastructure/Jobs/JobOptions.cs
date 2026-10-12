namespace SlotBook.SharedKernel.Infrastructure.Jobs;

/// <summary>Bound from <c>Jobs:*</c>.</summary>
internal sealed class JobOptions
{
    public const string LeaseSecondsKey = "Jobs:LeaseSeconds";

    public const string HeartbeatEnabledKey = "Jobs:Heartbeat:Enabled";

    public const string HeartbeatPeriodSecondsKey = "Jobs:Heartbeat:PeriodSeconds";

    public const string HeartbeatMaxTicksPerRunKey = "Jobs:Heartbeat:MaxTicksPerRun";

    public const int MinimumLeaseSeconds = 3;

    /// <summary>How long a job lease lives without renewal. It is renewed every third of it.</summary>
    public int LeaseSeconds { get; set; } = 30;

    /// <summary>Whether the sample heartbeat job is registered. Off by default.</summary>
    public bool HeartbeatEnabled { get; set; }

    /// <summary>Seconds between heartbeat ticks. Ticks are aligned to the epoch.</summary>
    public int HeartbeatPeriodSeconds { get; set; } = 60;

    /// <summary>The most ticks one run records; older missed ticks are skipped and logged.</summary>
    public int HeartbeatMaxTicksPerRun { get; set; } = 1440;
}
