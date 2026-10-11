using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using RabbitMQ.Client;
using Shouldly;
using SlotBook.IntegrationTests.TestSupport;
using Xunit;

namespace SlotBook.IntegrationTests.Infrastructure;

public class RealInfrastructureTests(MongoDatabaseFixture mongo, RabbitMqFixture rabbit)
    : IClassFixture<MongoDatabaseFixture>, IClassFixture<RabbitMqFixture>
{
    private static readonly TransactionOptions _majoritySnapshot = new(
        ReadConcern.Snapshot, ReadPreference.Primary, WriteConcern.WMajority);

    [Fact]
    public async Task TransactionCommitsTwoDocumentsAndMessageIsPublished()
    {
        var ct = TestContext.Current.CancellationToken;
        var bookings = mongo.Database.GetCollection<BsonDocument>("bookings");
        var audit = mongo.Database.GetCollection<BsonDocument>("audit");
        var id = ObjectId.GenerateNewId();

        // Collections must exist before they are touched inside a transaction.
        await mongo.Database.CreateCollectionAsync("bookings", cancellationToken: ct);
        await mongo.Database.CreateCollectionAsync("audit", cancellationToken: ct);

        using (var session = await mongo.Client.StartSessionAsync(cancellationToken: ct))
        {
            session.StartTransaction(_majoritySnapshot);
            await bookings.InsertOneAsync(session, new BsonDocument("_id", id), cancellationToken: ct);
            await audit.InsertOneAsync(session, new BsonDocument { { "_id", ObjectId.GenerateNewId() }, { "booking", id } }, cancellationToken: ct);
            await session.CommitTransactionAsync(ct);
        }

        (await bookings.CountDocumentsAsync(new BsonDocument("_id", id), cancellationToken: ct)).ShouldBe(1);
        (await audit.CountDocumentsAsync(new BsonDocument("booking", id), cancellationToken: ct)).ShouldBe(1);

        await using var channel = await rabbit.Connection.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            ct);
        var queue = (await channel.QueueDeclareAsync(
            queue: string.Empty, durable: false, exclusive: true, autoDelete: true, cancellationToken: ct)).QueueName;
        var body = Encoding.UTF8.GetBytes(id.ToString());

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: queue,
            mandatory: true,
            basicProperties: new BasicProperties { DeliveryMode = DeliveryModes.Persistent },
            body: body,
            cancellationToken: ct);

        var received = await channel.BasicGetAsync(queue, autoAck: true, ct);
        received.ShouldNotBeNull();
        Encoding.UTF8.GetString(received.Body.Span).ShouldBe(id.ToString());
    }

    [Fact]
    public async Task AbortedTransactionLeavesNoDocument()
    {
        var ct = TestContext.Current.CancellationToken;
        var items = mongo.Database.GetCollection<BsonDocument>("aborted");
        var id = ObjectId.GenerateNewId();
        await mongo.Database.CreateCollectionAsync("aborted", cancellationToken: ct);

        using var session = await mongo.Client.StartSessionAsync(cancellationToken: ct);
        session.StartTransaction(_majoritySnapshot);
        await items.InsertOneAsync(session, new BsonDocument("_id", id), cancellationToken: ct);
        await session.AbortTransactionAsync(ct);

        (await items.CountDocumentsAsync(new BsonDocument("_id", id), cancellationToken: ct)).ShouldBe(0);
    }

    [Fact]
    public async Task EachTestClassGetsItsOwnDatabase()
    {
        var ct = TestContext.Current.CancellationToken;
        mongo.DatabaseName.ShouldStartWith("it_");
        mongo.DatabaseName.Length.ShouldBeLessThanOrEqualTo(63);

        var other = new MongoDatabaseFixture();
        other.DatabaseName.ShouldNotBe(mongo.DatabaseName);
        await other.InitializeAsync();
        await other.Database.CreateCollectionAsync("probe", cancellationToken: ct);
        (await (await other.Client.ListDatabaseNamesAsync(ct)).ToListAsync(ct)).ShouldContain(other.DatabaseName);

        await other.DisposeAsync();

        using var check = new MongoClient(IntegrationEnvironment.MongoDb);
        (await (await check.ListDatabaseNamesAsync(ct)).ToListAsync(ct)).ShouldNotContain(other.DatabaseName);
    }
}
