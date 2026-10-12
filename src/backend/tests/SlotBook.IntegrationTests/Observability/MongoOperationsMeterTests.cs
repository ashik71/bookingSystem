using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Shouldly;
using SlotBook.IntegrationTests.TestSupport;
using Xunit;

namespace SlotBook.IntegrationTests.Observability;

public sealed class MongoOperationsMeterTests(MongoDatabaseFixture mongo) : IClassFixture<MongoDatabaseFixture>
{
    [Fact]
    public async Task InsertAndFindAreCountedByCommandNameOnly()
    {
        var ct = TestContext.Current.CancellationToken;
        var measurements = new List<(long Value, KeyValuePair<string, object?>[] Tags)>();
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Name == "SlotBook.MongoDB" && instrument.Name == "slotbook.mongodb.operations")
                {
                    l.EnableMeasurementEvents(instrument);
                }
            },
        };
        listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
        {
            lock (measurements)
            {
                measurements.Add((value, tags.ToArray()));
            }
        });
        listener.Start();

        using var factory = new SlotBookApiFactory { MongoDatabase = mongo.DatabaseName };
        var collection = factory.Services.GetRequiredService<IMongoDatabase>().GetCollection<BsonDocument>("meter_probe");
        await collection.InsertOneAsync(new BsonDocument("secret", "do-not-leak"), cancellationToken: ct);
        await collection.Find(new BsonDocument("secret", "do-not-leak")).ToListAsync(ct);

        (KeyValuePair<string, object?>[] Tags, long Value)[] seen;
        lock (measurements)
        {
            seen = [.. measurements.Select(m => (m.Tags, m.Value))];
        }

        seen.Sum(m => m.Value).ShouldBeGreaterThanOrEqualTo(2);
        seen.SelectMany(m => m.Tags).Select(t => t.Key).Distinct().ShouldBe(["db.operation.name"]);
        var names = seen.SelectMany(m => m.Tags).Select(t => t.Value as string).ToList();
        names.ShouldContain("insert");
        names.ShouldContain("find");
        names.ShouldAllBe(n => n != null && n.All(char.IsAsciiLetter));
    }
}
