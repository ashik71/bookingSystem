using Microsoft.Extensions.Logging;

namespace SlotBook.SharedKernel.Infrastructure.Persistence;

internal static partial class LoggerExtensions
{
    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "MongoDB readiness ping failed ({ExceptionType})")]
    internal static partial void LogMongoPingFailed(this ILogger logger, string exceptionType);

    [LoggerMessage(EventId = 4, Level = LogLevel.Debug, Message = "Aborting the MongoDB transaction failed; the server will time it out")]
    internal static partial void LogTransactionAbortFailed(this ILogger logger, Exception exception);

    [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Command {CommandType} stayed in conflict for {BudgetMs} ms after {Attempts} attempts; answering concurrency_conflict")]
    internal static partial void LogRetryExhausted(this ILogger logger, Exception exception, string commandType, int budgetMs, int attempts);

    [LoggerMessage(EventId = 6, Level = LogLevel.Warning, Message = "Command {CommandType}: the transaction commit result stayed unknown until the time budget ran out; the commit may have landed. Answering concurrency_conflict")]
    internal static partial void LogCommitResultUnknown(this ILogger logger, Exception exception, string commandType);
}
