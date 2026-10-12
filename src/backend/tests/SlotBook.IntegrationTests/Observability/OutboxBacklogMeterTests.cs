using System.Diagnostics.Metrics;
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
        await mongo.Database.GetCollection<BsonDocument>(OutboxBacklogMeter.MessageCollection)
            .InsertManyAsync([new BsonDocument("n", 1), new BsonDocument("n", 2), new BsonDocument("n", 3)], cancellationToken: ct);
        using var meter = new OutboxBacklogMeter(mongo.Database, MeterFactory());

        Observe().ShouldBe([3L]);
    }

    [Fact]
    public void GaugeRecordsNothingWhenMongoDbIsUnreachable()
    {
        using var client = new MongoClient("mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=300&connectTimeoutMS=300");
        using var meter = new OutboxBacklogMeter(client.GetDatabase("unreachable"), MeterFactory());

        Should.NotThrow(() => Observe().ShouldBeEmpty());
    }

    private static IMeterFactory MeterFactory() =>
        new ServiceCollection().AddMetrics().BuildServiceProvider().GetRequiredService<IMeterFactory>();

    private static List<long> Observe()
    {
        var values = new List<long>();
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Name == "slotbook.outbox.pending")
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
