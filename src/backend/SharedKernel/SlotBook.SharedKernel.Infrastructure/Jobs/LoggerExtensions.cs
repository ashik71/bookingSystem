using Microsoft.Extensions.Logging;

namespace SlotBook.SharedKernel.Infrastructure.Jobs;

internal static partial class LoggerExtensions
{
    [LoggerMessage(EventId = 30, Level = LogLevel.Debug, Message = "Job {Job} run started under lease holder {Holder}")]
    internal static partial void LogJobRunStarted(this ILogger logger, string job, string holder);

    [LoggerMessage(EventId = 31, Level = LogLevel.Debug, Message = "Job {Job} run finished")]
    internal static partial void LogJobRunFinished(this ILogger logger, string job);

    [LoggerMessage(EventId = 32, Level = LogLevel.Debug, Message = "Job {Job} lease is held by another host; skipping this tick")]
    internal static partial void LogJobLeaseHeld(this ILogger logger, string job);

    [LoggerMessage(EventId = 33, Level = LogLevel.Warning, Message = "Job {Job} lease was lost; the run was stopped")]
    internal static partial void LogJobLeaseLost(this ILogger logger, string job);

    [LoggerMessage(EventId = 34, Level = LogLevel.Error, Message = "Job {Job} run failed; the next tick runs again")]
    internal static partial void LogJobRunFailed(this ILogger logger, Exception exception, string job);

    [LoggerMessage(EventId = 35, Level = LogLevel.Warning, Message = "Job {Job} could not renew its lease ({ExceptionType}); retrying until the lease is about to expire")]
    internal static partial void LogJobRenewFailed(this ILogger logger, string job, string exceptionType);

    [LoggerMessage(EventId = 36, Level = LogLevel.Warning, Message = "Job {Job} could not release its lease ({ExceptionType}); it expires on its own")]
    internal static partial void LogJobReleaseFailed(this ILogger logger, string job, string exceptionType);

    [LoggerMessage(EventId = 37, Level = LogLevel.Information, Message = "Heartbeat tick {DueAt:O} processed at {ProcessedAt:O} ({Lateness} late)")]
    internal static partial void LogHeartbeatTick(this ILogger logger, DateTime dueAt, DateTime processedAt, TimeSpan lateness);

    [LoggerMessage(EventId = 38, Level = LogLevel.Warning, Message = "Heartbeat skipped {Skipped} missed ticks older than the cap of {Cap} per run")]
    internal static partial void LogHeartbeatTicksSkipped(this ILogger logger, int skipped, int cap);

    [LoggerMessage(EventId = 39, Level = LogLevel.Debug, Message = "Heartbeat ticks were already recorded by another run; nothing to do")]
    internal static partial void LogHeartbeatAlreadyRecorded(this ILogger logger);
}
