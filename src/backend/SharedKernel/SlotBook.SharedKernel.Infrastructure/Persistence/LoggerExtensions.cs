using Microsoft.Extensions.Logging;

namespace SlotBook.SharedKernel.Infrastructure.Persistence;

internal static partial class LoggerExtensions
{
    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "MongoDB readiness ping failed ({ExceptionType})")]
    internal static partial void LogMongoPingFailed(this ILogger logger, string exceptionType);
}
