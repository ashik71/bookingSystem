using System.Diagnostics.Metrics;
using MassTransit.MongoDbIntegration.Outbox;
using Microsoft.Extensions.Hosting;
using MongoDB.Driver;

namespace SlotBook.SharedKernel.Infrastructure.Messaging;

/// <summary>
/// Gauge <c>slotbook.outbox.pending</c>: outbox messages (bus outbox and consumer outbox) not yet delivered;
/// MassTransit removes a message once delivered. Counted on the <see cref="IMongoCollection{TDocument}"/> MassTransit
/// registers, so the collection name is the one it writes to. Read once per metric export. A failed read, such as
/// MongoDB being down, records nothing and never throws. Started as a hosted service only to keep the instrument
/// alive; it does no work at start.
/// </summary>
internal sealed class OutboxBacklogMeter : IHostedService, IDisposable
{
    internal const string MeterName = "SlotBook.Outbox";
    internal const string GaugeName = "slotbook.outbox.pending";

    private static readonly TimeSpan _readTimeout = TimeSpan.FromSeconds(2);

    private readonly IMongoCollection<OutboxMessage> _messages;
    private readonly Meter _meter;

    public OutboxBacklogMeter(IMongoCollection<OutboxMessage> messages, IMeterFactory meterFactory)
    {
        _messages = messages;
        _meter = meterFactory.Create(MeterName);
        _meter.CreateObservableGauge(GaugeName, Observe, unit: "{message}", description: "Outbox messages not yet delivered.");
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public void Dispose() => _meter.Dispose();

    private IEnumerable<Measurement<long>> Observe()
    {
        long pending;
        try
        {
            using var limit = new CancellationTokenSource(_readTimeout);
            pending = _messages.CountDocuments(FilterDefinition<OutboxMessage>.Empty, cancellationToken: limit.Token);
        }
#pragma warning disable CA1031 // a metric read must never throw into the exporter
        catch (Exception)
#pragma warning restore CA1031
        {
            return [];
        }

        return [new Measurement<long>(pending)];
    }
}
