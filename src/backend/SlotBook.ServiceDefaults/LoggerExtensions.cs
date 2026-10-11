using Microsoft.Extensions.Logging;

namespace SlotBook.ServiceDefaults;

internal static partial class LoggerExtensions
{
    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "{Method} {Path} responded {StatusCode}")]
    internal static partial void LogRequest(this ILogger logger, string method, string path, int statusCode);
}
