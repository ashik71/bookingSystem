using Microsoft.Extensions.Logging;

namespace SlotBook.SharedKernel.Infrastructure.Persistence;

internal static partial class LoggerExtensions
{
    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "MongoDB readiness ping failed ({ExceptionType})")]
    internal static partial void LogMongoPingFailed(this ILogger logger, string exceptionType);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Aborting the MongoDB transaction failed; the server will time it out")]
    internal static partial void LogTransactionAbortFailed(this ILogger logger, Exception exception);
}
