namespace SlotBook.SharedKernel.Infrastructure.Jobs;

/// <summary>The outcome of one <see cref="JobRunner.RunOnceAsync"/>.</summary>
internal enum JobRunResult
{
    Completed,
    LeaseHeld,
    LeaseLost,
    Failed,
}
