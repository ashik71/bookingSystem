#pragma warning disable CA1812 // created by the container
using System.Diagnostics;
using SlotBook.SharedKernel.Infrastructure.Jobs;
using SlotBook.SharedKernel.Infrastructure.Persistence.Leases;

namespace SlotBook.IntegrationTests.TestSupport.Jobs;

/// <summary>A timed job that only waits and records when it ran and under which host.</summary>
internal sealed class RecordingJob(RecordingJobControl control, LeaseHolder holder) : ITimedJob
{
    public string Name => control.Name;

    public TimeSpan Period => control.Period;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var start = Stopwatch.GetTimestamp();
        control.NextRun();
        try
        {
            if (control.ShouldFail())
            {
                throw new InvalidOperationException("The recording job was told to fail.");
            }

            await Task.Delay(control.RunDuration, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            control.CancelledHolders.Enqueue(holder.Id);
            throw;
        }
        finally
        {
            control.Runs.Enqueue(new RecordedRun(holder.Id, start, Stopwatch.GetTimestamp()));
        }
    }
}
