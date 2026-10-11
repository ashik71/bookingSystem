#pragma warning disable CA1515 // public: used as an xunit fixture type, or discovered by MVC
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace SlotBook.IntegrationTests.TestSupport;

public sealed class CapturingLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly ConcurrentQueue<CapturedLog> _records = new();
    private IExternalScopeProvider _scopes = new LoggerExternalScopeProvider();

    public IReadOnlyCollection<CapturedLog> Records => _records;

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, this);

    public void SetScopeProvider(IExternalScopeProvider scopeProvider) => _scopes = scopeProvider;

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(string category, CapturingLoggerProvider owner) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => owner._scopes.Push(state);

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            string? traceId = null;
            owner._scopes.ForEachScope(
                (scope, _) =>
                {
                    if (scope is IEnumerable<KeyValuePair<string, object?>> pairs)
                    {
                        foreach (var pair in pairs)
                        {
                            if (string.Equals(pair.Key, "TraceId", StringComparison.Ordinal))
                            {
                                traceId = pair.Value?.ToString();
                            }
                        }
                    }
                },
                state);
            owner._records.Enqueue(new CapturedLog(category, eventId.Id, formatter(state, exception), traceId));
        }
    }
}
