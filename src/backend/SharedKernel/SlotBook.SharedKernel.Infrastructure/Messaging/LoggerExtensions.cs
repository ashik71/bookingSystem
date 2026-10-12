using Microsoft.Extensions.Logging;

namespace SlotBook.SharedKernel.Infrastructure.Messaging;

internal static partial class LoggerExtensions
{
    [LoggerMessage(EventId = 20, Level = LogLevel.Debug, Message = "Message {MessageId} on {Endpoint} was already recorded in the inbox; the consumer did not run")]
    internal static partial void LogConsumeDeduplicated(this ILogger logger, Guid? messageId, string endpoint);
}
