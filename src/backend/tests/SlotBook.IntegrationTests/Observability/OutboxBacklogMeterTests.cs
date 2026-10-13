using System.Diagnostics.Metrics;
using MassTransit.MongoDbIntegration.Outbox;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Shouldly;
using SlotBook.IntegrationTests.TestSupport;
using SlotBook.SharedKernel.Infrastructure.Messaging;
using Xunit;

namespace SlotBook.IntegrationTests.Observability;

public sealed class OutboxBacklogMeterTests(MongoDatabaseFixture mongo) : IClassFixture<MongoDatabaseFixture>
{
    [Fact]
    public async Task GaugeReadsThePendingCount()
    {
        var ct = TestContext.Current.CancellationToken;
        await using var host = new SlotBookApiFactory { MongoDatabase = mongo.DatabaseName };
        var collection = host.Services.GetRequiredService<IMongoCollection<OutboxMessage>>();

        // Insert through the namespace MassTransit registered, as untyped documents: the gauge only counts them.
        await mongo.Client.GetDatabase(collection.CollectionNamespace.DatabaseNamespace.DatabaseName)
            .GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName)
            .InsertManyAsync([new BsonDocument("n", 1), new BsonDocument("n", 2), new BsonDocument("n", 3)], cancellationToken: ct);

        Observe(host.Services.GetRequiredService<IMeterFactory>()).ShouldBe([3L]);
    }

    [Fact]
    public void GaugeRecordsNothingWhenMongoDbIsUnreachable()
    {
        using var client = new MongoClient("mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=300&connectTimeoutMS=300");
        var collection = client.GetDatabase("unreachable").GetCollection<OutboxMessage>("outbox.messages");
        var factory = new ServiceCollection().AddMetrics().BuildServiceProvider().GetRequiredService<IMeterFactory>();
        using var meter = new OutboxBacklogMeter(collection, factory);

        Should.NotThrow(() => Observe(factory).ShouldBeEmpty());
    }

    private static List<long> Observe(IMeterFactory factory)
    {
        var values = new List<long>();
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Name == OutboxBacklogMeter.GaugeName && ReferenceEquals(instrument.Meter.Scope, factory))
                {
                    l.EnableMeasurementEvents(instrument);
                }
            },
        };
        listener.SetMeasurementEventCallback<long>((_, value, _, _) => values.Add(value));
        listener.Start();
        listener.RecordObservableInstruments();
        return values;
    }
}
