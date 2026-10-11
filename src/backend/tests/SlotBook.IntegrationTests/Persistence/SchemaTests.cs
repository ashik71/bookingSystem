using System.Collections.Concurrent;
using System.Diagnostics;
#pragma warning disable CA1812 // test doubles are created by the container
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Shouldly;
using SlotBook.IntegrationTests.TestSupport;
using SlotBook.SharedKernel.Infrastructure.Persistence;
using SlotBook.SharedKernel.Infrastructure.Persistence.Migrations;
using Xunit;

namespace SlotBook.IntegrationTests.Persistence;

public sealed class SchemaTests(MongoDatabaseFixture mongo) : IClassFixture<MongoDatabaseFixture>
{
    private const string Samples = "schema_samples";

    [Fact]
    public async Task IndexesExistAfterStartupAndARestartIsANoOp()
    {
        var ct = TestContext.Current.CancellationToken;
        var database = NewDatabase();

        Start(NewFactory(database, new MigrationLog()));

        var first = await IndexNamesAsync(database, ct);
        first.ShouldBe(["_id_", "ix_schema_samples_kind", "ux_schema_samples_key"], ignoreOrder: true);

        Start(NewFactory(database, new MigrationLog()));

        (await IndexNamesAsync(database, ct)).ShouldBe(first, ignoreOrder: true);
        var unique = await IndexAsync(database, "ux_schema_samples_key", ct);
        unique["unique"].AsBoolean.ShouldBeTrue();
    }

    [Fact]
    public async Task TheRetryableIndexesOfAConfigurationAreRegistered()
    {
        using var factory = NewFactory(NewDatabase(), new MigrationLog());
        using var client = factory.CreateClient();

        var keys = factory.Services.GetRequiredService<RetryableDuplicateKeys>();

        keys.Contains(Samples, "ux_schema_samples_key").ShouldBeTrue();
        keys.Contains(Samples, "ix_schema_samples_kind").ShouldBeFalse();
    }

    [Fact]
    public async Task TwoHostsStartingTogetherApplyEachMigrationOnceInOrder()
    {
        var ct = TestContext.Current.CancellationToken;
        var database = NewDatabase();
        var log = new MigrationLog();

        using var first = NewFactory(database, log);
        using var second = NewFactory(database, log);
        await Task.WhenAll(Task.Run(() => first.CreateClient().Dispose(), ct), Task.Run(() => second.CreateClient().Dispose(), ct));

        log.Ran.ToArray().ShouldBe(["0001-first", "0002-second"]);
        var recorded = await mongo.Client.GetDatabase(database).GetCollection<BsonDocument>("migrations")
            .Find(FilterDefinition<BsonDocument>.Empty).SortBy(document => document["_id"]).ToListAsync(ct);
        recorded.Select(document => document["_id"].AsString).ShouldBe(["0001-first", "0002-second"]);
        (await mongo.Client.GetDatabase(database).GetCollection<BsonDocument>("leases").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: ct))
            .ShouldBe(0, "the lock is released when the migrations finish");
    }

    [Fact]
    public async Task ARestartDoesNotRunRecordedMigrationsAgain()
    {
        var database = NewDatabase();
        var log = new MigrationLog();

        Start(NewFactory(database, log));

        Start(NewFactory(database, log));

        log.Ran.ToArray().ShouldBe(["0001-first", "0002-second"]);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task AnExpiredLockDoesNotBlockStartup()
    {
        var ct = TestContext.Current.CancellationToken;
        var database = NewDatabase();
        await SeedLockAsync(database, Now().AddSeconds(-1), ct);
        var log = new MigrationLog();
        var clock = Stopwatch.StartNew();

        Start(NewFactory(database, log));

        clock.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(10));
        log.Ran.ToArray().ShouldBe(["0001-first", "0002-second"]);
    }

    [Fact]
    public async Task ALiveLockMakesStartupWaitUntilItExpires()
    {
        var ct = TestContext.Current.CancellationToken;
        var database = NewDatabase();
        await SeedLockAsync(database, Now().AddSeconds(3), ct);
        var log = new MigrationLog();
        var clock = Stopwatch.StartNew();

        Start(NewFactory(database, log));

        clock.Elapsed.ShouldBeGreaterThan(TimeSpan.FromSeconds(2));
        log.Ran.ToArray().ShouldBe(["0001-first", "0002-second"]);
    }

    [Fact]
    public void StartupFailsWhenTheSchemaTakesLongerThanTheTimeout()
    {
        var database = NewDatabase();
        var log = new MigrationLog { FirstTakes = TimeSpan.FromSeconds(30) };
        using var factory = NewFactory(database, log, new Dictionary<string, string?> { ["Mongo:SchemaTimeoutSeconds"] = "1" });

        var exception = Should.Throw<Exception>(() => factory.CreateClient());

        exception.ToString().ShouldContain("schema was not ready");
    }

    private static DateTime Now()
    {
#pragma warning disable RS0030 // a test seeds a lock against the real clock
        return DateTime.UtcNow;
#pragma warning restore RS0030
    }

    private static void Start(SlotBookApiFactory factory)
    {
        using (factory)
        {
            factory.CreateClient().Dispose();
        }
    }

    private static string NewDatabase() => MongoDatabaseFixture.NewDatabaseName("SchemaTests");

    private static SlotBookApiFactory NewFactory(string database, MigrationLog log, IReadOnlyDictionary<string, string?>? settings = null) =>
        new()
        {
            MongoDatabase = database,
            Settings = settings ?? new Dictionary<string, string?>(),
            ConfigureTestServices = services =>
            {
                services.AddSingleton(log);
                services.AddSingleton<IIndexConfiguration, SampleIndexes>();
                services.AddSingleton<IMigration, FirstMigration>();
                services.AddSingleton<IMigration, SecondMigration>();
            },
        };

    private async Task SeedLockAsync(string database, DateTime expiresAt, CancellationToken ct)
    {
        var db = mongo.Client.GetDatabase(database);
        await db.CreateCollectionAsync("leases", cancellationToken: ct);
        await db.GetCollection<BsonDocument>("leases").InsertOneAsync(
            new BsonDocument { { "_id", "migrations" }, { "holder", "crashed" }, { "expiresAt", expiresAt } }, cancellationToken: ct);
    }

    private async Task<List<string>> IndexNamesAsync(string database, CancellationToken ct)
    {
        var indexes = await (await mongo.Client.GetDatabase(database).GetCollection<BsonDocument>(Samples).Indexes.ListAsync(ct)).ToListAsync(ct);
        return [.. indexes.Select(index => index["name"].AsString)];
    }

    private async Task<BsonDocument> IndexAsync(string database, string name, CancellationToken ct)
    {
        var indexes = await (await mongo.Client.GetDatabase(database).GetCollection<BsonDocument>(Samples).Indexes.ListAsync(ct)).ToListAsync(ct);
        return indexes.Single(index => index["name"].AsString == name);
    }

    private sealed class MigrationLog
    {
        public ConcurrentQueue<string> Ran { get; } = new();

        public TimeSpan FirstTakes { get; init; } = TimeSpan.FromMilliseconds(500);
    }

    private sealed class SampleIndexes : IIndexConfiguration
    {
        public string CollectionName => Samples;

        public IReadOnlyList<CreateIndexModel<BsonDocument>> Indexes { get; } =
        [
            new(Builders<BsonDocument>.IndexKeys.Ascending("tenantId").Ascending("key"), new CreateIndexOptions { Name = "ux_schema_samples_key", Unique = true }),
            new(Builders<BsonDocument>.IndexKeys.Ascending("tenantId").Ascending("kind"), new CreateIndexOptions { Name = "ix_schema_samples_kind" }),
        ];

        public IReadOnlyCollection<string> RetryableIndexes { get; } = ["ux_schema_samples_key"];
    }

    private sealed class FirstMigration(MigrationLog log) : IMigration
    {
        public string Id => "0001-first";

        public async Task UpAsync(IMongoDatabase database, CancellationToken cancellationToken)
        {
            // Long enough for a second host to reach the lock while this one holds it.
            await Task.Delay(log.FirstTakes, cancellationToken);
            log.Ran.Enqueue(Id);
        }
    }

    private sealed class SecondMigration(MigrationLog log) : IMigration
    {
        public string Id => "0002-second";

        public Task UpAsync(IMongoDatabase database, CancellationToken cancellationToken)
        {
            log.Ran.Enqueue(Id);
            return Task.CompletedTask;
        }
    }
}
