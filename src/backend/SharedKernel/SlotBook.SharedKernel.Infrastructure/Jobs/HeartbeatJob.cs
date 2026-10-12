using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SlotBook.SharedKernel.Infrastructure.Persistence.Leases;

namespace SlotBook.SharedKernel.Infrastructure.Jobs;

/// <summary>
/// The sample catch-up job (AD-11). Every run records each epoch-aligned tick in <c>(lastTickAt, now]</c> in one compare-and-set
/// update, so a restart or a gap is caught up in the first run and no tick is ever recorded twice.
/// </summary>
internal sealed class HeartbeatJob(HeartbeatStateStore state, IOptions<JobOptions> options, TimeProvider time, ILogger<HeartbeatJob> logger) : ITimedJob
{
    public string Name => "heartbeat";

    public TimeSpan Period => TimeSpan.FromSeconds(options.Value.HeartbeatPeriodSeconds);

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var last = await state.ReadLastTickAtAsync(cancellationToken).ConfigureAwait(false);
        var (due, skipped) = HeartbeatTicks.Due(last, now, Period, options.Value.HeartbeatMaxTicksPerRun);
        if (due.Count == 0)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (!await state.TryRecordAsync(last, due, now, cancellationToken).ConfigureAwait(false))
        {
            logger.LogHeartbeatAlreadyRecorded();
            return;
        }

        if (skipped > 0)
        {
            logger.LogHeartbeatTicksSkipped(skipped, options.Value.HeartbeatMaxTicksPerRun);
        }

        foreach (var tick in due)
        {
            logger.LogHeartbeatTick(tick, now, now - tick);
        }
    }
}
