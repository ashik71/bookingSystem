namespace SlotBook.Api.Errors;

internal static partial class LoggerExtensions
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Unhandled exception")]
    internal static partial void LogUnhandledException(this ILogger logger, Exception exception);
}
