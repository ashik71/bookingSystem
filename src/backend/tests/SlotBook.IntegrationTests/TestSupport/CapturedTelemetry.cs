#pragma warning disable CA1515 // public: used as an xunit fixture type, or discovered by MVC
using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace SlotBook.IntegrationTests.TestSupport;

/// <summary>
/// In-memory exporters hooked into the application's real OpenTelemetry providers, so tests see
/// what would leave the process after the scrubbing processor has run.
/// </summary>
public sealed class CapturedTelemetry
{
    private static readonly TimeSpan _spanWaitLimit = TimeSpan.FromSeconds(5);

    private readonly List<Activity> _spans = [];
    private readonly List<Metric> _metrics = [];
    private readonly ConcurrentQueue<string> _logRecords = new();

    public IReadOnlyCollection<Activity> Spans
    {
        get
        {
            lock (_spans)
            {
                return _spans.ToList();
            }
        }
    }

    /// <summary>Every log record as text: message, body, attributes and exception.</summary>
    public IReadOnlyCollection<string> LogRecords => _logRecords;

    /// <summary>
    /// Waits for a span matching <paramref name="match"/>. With <c>TestServer</c> the server span stops after the client
    /// already has the response, so a test must not read <see cref="Spans"/> straight after the call. Polls every 20 ms
    /// for up to 5 s and fails with a clear message when nothing matches.
    /// </summary>
    public async Task<Activity> WaitForSpanAsync(Func<Activity, bool> match, CancellationToken ct)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(_spanWaitLimit);
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(20));
        do
        {
            var found = Spans.FirstOrDefault(match);
            if (found is not null)
            {
                return found;
            }
        }
        while (await WaitAsync(timer, timeout.Token));

        ct.ThrowIfCancellationRequested();
        throw new TimeoutException($"No exported span matched within {_spanWaitLimit.TotalSeconds:0} s. Exported: {string.Join(", ", Spans.Select(s => s.DisplayName))}.");
    }

    public void Attach(IServiceCollection services)
    {
        services.ConfigureOpenTelemetryTracerProvider((_, tracing) => tracing.AddInMemoryExporter(_spans));
        services.ConfigureOpenTelemetryMeterProvider((_, metrics) => metrics.AddInMemoryExporter(_metrics));
        services.Configure<OpenTelemetryLoggerOptions>(options =>
            options.AddProcessor(new SimpleLogRecordExportProcessor(new TextLogExporter(_logRecords))));
    }

    /// <summary>Collects the metrics now and returns every metric point's tags as <c>key=value</c> text.</summary>
    public IReadOnlyList<string> CollectMetricTags(IServiceProvider services)
    {
        services.GetRequiredService<MeterProvider>().ForceFlush(10_000);
        var tags = new List<string>();
        lock (_metrics)
        {
            foreach (var metric in _metrics)
            {
                foreach (var point in metric.GetMetricPoints())
                {
                    foreach (var tag in point.Tags)
                    {
                        tags.Add($"{tag.Key}={tag.Value}");
                    }
                }
            }
        }

        return tags;
    }

    /// <summary>Every span tag, event tag and event name as <c>key=value</c> text.</summary>
    public IReadOnlyList<string> SpanTexts() =>
        Spans.SelectMany(span =>
            span.TagObjects.Select(tag => $"{tag.Key}={tag.Value}")
                .Concat(span.Events.SelectMany(e => e.Tags.Select(tag => $"{tag.Key}={tag.Value}").Append(e.Name)))
                .Append(span.DisplayName))
            .ToList();

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken ct)
    {
        try
        {
            return await timer.WaitForNextTickAsync(ct);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private sealed class TextLogExporter(ConcurrentQueue<string> sink) : BaseExporter<LogRecord>
    {
        public override ExportResult Export(in Batch<LogRecord> batch)
        {
            foreach (var record in batch)
            {
                var attributes = record.Attributes?.Select(a => $"{a.Key}={a.Value}") ?? [];
                sink.Enqueue(string.Join(' ', [record.FormattedMessage, record.Body, record.Exception?.ToString(), .. attributes]));
            }

            return ExportResult.Success;
        }
    }
}
