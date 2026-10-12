#pragma warning disable CA1031 // the runner never stops the host: every failure of a run, a renewal or a release is logged and the next tick runs
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SlotBook.SharedKernel.Infrastructure.Persistence.Leases;

namespace SlotBook.SharedKernel.Infrastructure.Jobs;

/// <summary>
/// Runs one <see cref="ITimedJob"/> (AD-11): once at start, which is the catch-up after a restart, then every period.
/// Each run takes the job's lease first, renews it while the run lasts, cancels the run when the lease is lost and
/// releases it at the end. Every failure is logged and never stops the host.
/// </summary>
internal sealed class JobRunner(
    ITimedJob job,
    LeaseStore leases,
    LeaseHolder holder,
    IOptions<JobOptions> options,
    TimeProvider time,
    ILogger<JobRunner> logger) : BackgroundService
{
    private static readonly TimeSpan _releaseTimeout = TimeSpan.FromSeconds(5);

    public string JobName => job.Name;

    internal ITimedJob Job => job;

    private TimeSpan LeaseDuration => TimeSpan.FromSeconds(options.Value.LeaseSeconds);

    /// <summary>Runs the job once under its lease. Never throws, except <see cref="OperationCanceledException"/> when the host stops.</summary>
    public async Task<JobRunResult> RunOnceAsync(CancellationToken cancellationToken)
    {
        Lease? lease;
        try
        {
            lease = await leases.TryAcquireAsync("job:" + job.Name, LeaseDuration, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogJobRunFailed(exception, job.Name);
            return JobRunResult.Failed;
        }

        if (lease is null)
        {
            logger.LogJobLeaseHeld(job.Name);
            return JobRunResult.LeaseHeld;
        }

        using var runCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var leaseLost = false;
#pragma warning disable CA2025 // the renewal task is awaited in the finally block, before the source is disposed
        var renewal = RenewWhileRunningAsync(lease, runCancellation, () => leaseLost = true);
#pragma warning restore CA2025
        var result = JobRunResult.Completed;
        try
        {
            logger.LogJobRunStarted(job.Name, holder.Id);
            await job.RunAsync(runCancellation.Token).ConfigureAwait(false);
            logger.LogJobRunFinished(job.Name);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException) when (Volatile.Read(ref leaseLost))
        {
            result = JobRunResult.LeaseLost;
        }
        catch (Exception exception)
        {
            logger.LogJobRunFailed(exception, job.Name);
            result = JobRunResult.Failed;
        }
        finally
        {
            await runCancellation.CancelAsync().ConfigureAwait(false);
            await renewal.ConfigureAwait(false);
            await ReleaseAsync(lease).ConfigureAwait(false);
        }

        if (Volatile.Read(ref leaseLost))
        {
            logger.LogJobLeaseLost(job.Name);
            return JobRunResult.LeaseLost;
        }

        return result;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Let the host finish starting (schema initializer first) before the first, catch-up run.
            await Task.Yield();
            await RunOnceAsync(stoppingToken).ConfigureAwait(false);

            using var timer = new PeriodicTimer(job.Period, time);
            while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
            {
                await RunOnceAsync(stoppingToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // The host is stopping.
        }
    }

    /// <summary>
    /// Renews every third of the lease. A refused renewal cancels the run at once. A renewal that errors (MongoDB
    /// unreachable) is retried, and the run is cancelled one renew interval before the lease expires.
    /// </summary>
    private async Task RenewWhileRunningAsync(Lease lease, CancellationTokenSource run, Action markLost)
    {
        var interval = LeaseDuration / 3;
        var retry = interval / 4;
        var delay = interval;
        var token = run.Token;
        try
        {
            while (true)
            {
                await Task.Delay(delay, time, token).ConfigureAwait(false);
                try
                {
                    if (!await leases.RenewAsync(lease, LeaseDuration, token).ConfigureAwait(false))
                    {
                        markLost();
                        await run.CancelAsync().ConfigureAwait(false);
                        return;
                    }

                    delay = interval;
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    logger.LogJobRenewFailed(job.Name, exception.GetType().Name);
                    if (time.GetUtcNow().UtcDateTime >= lease.ExpiresAt - interval)
                    {
                        markLost();
                        await run.CancelAsync().ConfigureAwait(false);
                        return;
                    }

                    delay = retry;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // The run ended.
        }
    }

    private async Task ReleaseAsync(Lease lease)
    {
        try
        {
            using var timeout = new CancellationTokenSource(_releaseTimeout, time);
            await leases.ReleaseAsync(lease, timeout.Token).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            logger.LogJobReleaseFailed(job.Name, exception.GetType().Name);
        }
    }
}
