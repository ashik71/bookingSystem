using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Shouldly;
using SlotBook.IntegrationTests.TestSupport;
using SlotBook.IntegrationTests.TestSupport.Jobs;
using SlotBook.SharedKernel.Infrastructure.Jobs;
using SlotBook.SharedKernel.Infrastructure.Persistence.Leases;
using Xunit;

namespace SlotBook.IntegrationTests.Jobs;

public sealed class JobRunnerTests(MongoDatabaseFixture mongo) : IClassFixture<MongoDatabaseFixture>
{
    private const string UnreachableMongo = "mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=500&connectTimeoutMS=500";
    private const int LeaseHeldEvent = 32;
    private const int LeaseLostEvent = 33;
    private const int RunFailedEvent = 34;

    private static Dictionary<string, string?> Settings { get; } = new()
    {
        ["Jobs:LeaseSeconds"] = "3",
        ["Logging:LogLevel:Default"] = "Debug",
    };

    [Fact]
    public async Task TwoHostsNeverRunAJobAtTheSameTime()
    {
        var control = NewControl(period: 200, run: 300);
        await using var first = NewHost(control);
        await using var second = NewHost(control);
        first.CreateClient().Dispose();
        second.CreateClient().Dispose();

        var holders = new[] { HolderOf(first), HolderOf(second) };
        await Task.Delay(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        await first.DisposeAsync();
        await second.DisposeAsync();

        var runs = control.Runs.OrderBy(run => run.Start).ToList();
        runs.Count.ShouldBeGreaterThan(2);
        for (var index = 1; index < runs.Count; index++)
        {
            runs[index].Start.ShouldBeGreaterThanOrEqualTo(runs[index - 1].End, "a run started while the previous run still held the lease");
        }

        runs.Select(run => run.Holder).Distinct().ShouldAllBe(holder => holders.Contains(holder));
    }

    [Fact]
    public async Task WhileARunLastsTheLeaseNamesItsHostAndTheOtherHostSkips()
    {
        var ct = TestContext.Current.CancellationToken;
        var control = NewControl(period: 200, run: 2500);
        using var first = NewHost(control);
        using var second = NewHost(control);
        first.CreateClient().Dispose();
        second.CreateClient().Dispose();

        await WaitForAsync(() => control.Started > 0, ct);
        var lease = await LeaseAsync(control, ct);

        var running = lease["holder"].AsString;
        running.ShouldBeOneOf(HolderOf(first), HolderOf(second));
        lease["expiresAt"].ToUniversalTime().ShouldBeGreaterThan(Now());
        var other = running == HolderOf(first) ? second : first;
        await WaitForAsync(() => other.Logs.Records.Any(log => log.EventId == LeaseHeldEvent), ct);
    }

    [Fact]
    public async Task ALongRunKeepsItsLeaseByRenewing()
    {
        var ct = TestContext.Current.CancellationToken;
        var control = NewControl(period: 200, run: 5000);
        using var first = NewHost(control);
        using var second = NewHost(control);
        first.CreateClient().Dispose();
        second.CreateClient().Dispose();

        await WaitForAsync(() => control.Started > 0, ct);
        var initial = (await LeaseAsync(control, ct))["expiresAt"].ToUniversalTime();
        await Task.Delay(TimeSpan.FromMilliseconds(2500), ct);
        var renewed = (await LeaseAsync(control, ct))["expiresAt"].ToUniversalTime();

        renewed.ShouldBeGreaterThan(initial);
        control.Started.ShouldBe(1, "the lease is 3 s, the run 5 s: only renewal keeps the other host out");
        await WaitForAsync(() => control.Runs.Count == 1, ct);
        control.CancelledHolders.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public async Task ARunStopsWhenItsLeaseIsTaken()
    {
        var ct = TestContext.Current.CancellationToken;
        var control = NewControl(period: 200, run: 60_000);
        using var host = NewHost(control);
        host.CreateClient().Dispose();
        await WaitForAsync(() => control.Started > 0, ct);

        await mongo.Database.GetCollection<BsonDocument>("leases").UpdateOneAsync(
            Builders<BsonDocument>.Filter.Eq("_id", "job:" + control.Name),
            Builders<BsonDocument>.Update.Set("holder", "thief").Set("expiresAt", Now().AddMinutes(10)),
            cancellationToken: ct);

        await WaitForAsync(() => control.CancelledHolders.Count == 1, ct);
        await WaitForAsync(() => host.Logs.Records.Any(log => log.EventId == LeaseLostEvent), ct);
        (await LeaseAsync(control, ct))["holder"].AsString.ShouldBe("thief", "a lost run must not release the new holder's lease");
    }

    [Fact]
    public async Task AnExpiredLeaseIsTakenOver()
    {
        var ct = TestContext.Current.CancellationToken;
        var control = NewControl(period: 200, run: 50);
        await mongo.Database.GetCollection<BsonDocument>("leases").InsertOneAsync(
            new BsonDocument { { "_id", "job:" + control.Name }, { "holder", "crashed" }, { "expiresAt", Now().AddSeconds(-1) } }, cancellationToken: ct);
        using var host = NewHost(control);
        host.CreateClient().Dispose();

        await WaitForAsync(() => !control.Runs.IsEmpty, ct);

        control.Runs.First().Holder.ShouldBe(HolderOf(host));
    }

    [Fact]
    public async Task AFailingRunIsLoggedAndTheNextTickRuns()
    {
        var ct = TestContext.Current.CancellationToken;
        var control = new RecordingJobControl { Name = NewName(), Period = TimeSpan.FromMilliseconds(200), RunDuration = TimeSpan.FromMilliseconds(50), FailFirstRuns = 1 };
        using var host = NewHost(control);
        using var client = host.CreateClient();

        await WaitForAsync(() => control.Started >= 2, ct);

        host.Logs.Records.ShouldContain(log => log.EventId == RunFailedEvent);
        (await client.GetAsync("/health", ct)).StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        await WaitForAsync(() => host.Logs.Records.Count(log => log.EventId == RunFailedEvent) == 1 && control.Runs.Count >= 2, ct);
    }

    [Fact]
    public async Task TheHostKeepsServingLivenessWhenMongoDbIsDown()
    {
        var ct = TestContext.Current.CancellationToken;
        var control = NewControl(period: 200, run: 50);
        using var host = NewHost(control, unreachable: true);
        using var client = host.CreateClient();

        await WaitForAsync(() => host.Logs.Records.Any(log => log.EventId == RunFailedEvent), ct);

        (await client.GetAsync("/health", ct)).StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        control.Started.ShouldBe(0);
    }

    private static string HolderOf(SlotBookApiFactory host) => host.Services.GetRequiredService<LeaseHolder>().Id;

    private static string NewName() => "recording-" + Convert.ToHexStringLower(System.Security.Cryptography.RandomNumberGenerator.GetBytes(4));

    private static DateTime Now()
    {
#pragma warning disable RS0030 // a test reads the lease against the real clock
        return DateTime.UtcNow;
#pragma warning restore RS0030
    }

    private static async Task WaitForAsync(Func<bool> condition, CancellationToken ct)
    {
        var clock = Stopwatch.StartNew();
        while (!condition())
        {
            clock.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(20), "the condition was not met in time");
            await Task.Delay(25, ct);
        }
    }

    private static RecordingJobControl NewControl(int period, int run) =>
        new() { Name = NewName(), Period = TimeSpan.FromMilliseconds(period), RunDuration = TimeSpan.FromMilliseconds(run) };

    private static void AddJob(IServiceCollection services, RecordingJobControl control)
    {
        services.AddSingleton(control);
        services.AddTimedJob<RecordingJob>();
    }

    private async Task<BsonDocument> LeaseAsync(RecordingJobControl control, CancellationToken ct) =>
        await mongo.Database.GetCollection<BsonDocument>("leases")
            .Find(Builders<BsonDocument>.Filter.Eq("_id", "job:" + control.Name)).SingleAsync(ct);

    private SlotBookApiFactory NewHost(RecordingJobControl control, bool unreachable = false) =>
        unreachable
            ? new SlotBookApiFactory
            {
                MongoConnectionString = UnreachableMongo,
                SkipSchemaInitializer = true,
                Settings = Settings,
                ConfigureTestServices = services => AddJob(services, control),
            }
            : new SlotBookApiFactory
            {
                MongoDatabase = mongo.DatabaseName,
                Settings = Settings,
                ConfigureTestServices = services => AddJob(services, control),
            };
}
