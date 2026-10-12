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
}
