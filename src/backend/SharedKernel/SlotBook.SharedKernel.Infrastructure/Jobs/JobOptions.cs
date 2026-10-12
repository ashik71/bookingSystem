namespace SlotBook.SharedKernel.Infrastructure.Jobs;

/// <summary>Bound from <c>Jobs:*</c>.</summary>
internal sealed class JobOptions
{
    public const string LeaseSecondsKey = "Jobs:LeaseSeconds";

    public const int MinimumLeaseSeconds = 3;

    /// <summary>How long a job lease lives without renewal. It is renewed every third of it.</summary>
    public int LeaseSeconds { get; set; } = 30;
}
