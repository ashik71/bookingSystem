using System.Diagnostics.Metrics;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver.Core.Events;

namespace SlotBook.SharedKernel.Infrastructure.Persistence;

/// <summary>
/// Counts MongoDB commands sent, for the free-tier operations alert (AD-18). The only tag is the command name
/// (<c>db.operation.name</c>); command text and namespaces never reach a metric (AD-17).
/// </summary>
internal sealed class MongoOperationsMeter : IDisposable
{
    internal const string MeterName = "SlotBook.MongoDB";
    internal const string CounterName = "slotbook.mongodb.operations";
    internal const string OperationTag = "db.operation.name";

    private readonly Meter _meter;
    private readonly Counter<long> _operations;

    public MongoOperationsMeter(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);
        _operations = _meter.CreateCounter<long>(CounterName, unit: "{operation}", description: "MongoDB commands sent, by command name.");
    }

    public void Subscribe(ClusterBuilder builder) =>
        builder.Subscribe<CommandStartedEvent>(started =>
            _operations.Add(1, new KeyValuePair<string, object?>(OperationTag, started.CommandName)));

    public void Dispose() => _meter.Dispose();
}
