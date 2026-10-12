using System.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson;
using MongoDB.Driver;

namespace SlotBook.SharedKernel.Infrastructure.Messaging;

/// <summary>
/// Gauge <c>slotbook.outbox.pending</c>: bus-outbox messages not yet handed to the broker (MassTransit removes a
/// message once delivered). Read once per metric export. A failed read, such as MongoDB being down, records
/// nothing and never throws. Started as a hosted service only to keep the instrument alive; it does no work at start.
/// </summary>
internal sealed class OutboxBacklogMeter : IHostedService, IDisposable
{
    internal const string MeterName = "SlotBook.Outbox";
    internal const string GaugeName = "slotbook.outbox.pending";
    internal const string MessageCollection = "outbox.messages";

    private static readonly TimeSpan _readTimeout = TimeSpan.FromSeconds(2);

    private readonly IMongoDatabase _database;
    private readonly Meter _meter;

    public OutboxBacklogMeter(IMongoDatabase database, IMeterFactory meterFactory)
    {
        _database = database;
        _meter = meterFactory.Create(MeterName);
        _meter.CreateObservableGauge(GaugeName, Observe, unit: "{message}", description: "Bus-outbox messages not yet delivered.");
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
            pending = _database.GetCollection<BsonDocument>(MessageCollection)
                .CountDocuments(FilterDefinition<BsonDocument>.Empty, cancellationToken: limit.Token);
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
