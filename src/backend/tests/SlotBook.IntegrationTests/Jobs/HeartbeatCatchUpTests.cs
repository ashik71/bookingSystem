using System.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using Shouldly;
using SlotBook.IntegrationTests.TestSupport;
using Xunit;

namespace SlotBook.IntegrationTests.Jobs;

public sealed class HeartbeatCatchUpTests(MongoDatabaseFixture mongo) : IClassFixture<MongoDatabaseFixture>
{
    private static readonly FilterDefinition<BsonDocument> _stateFilter = Builders<BsonDocument>.Filter.Eq("_id", "heartbeat:state");

    [Fact]
    public async Task TwoHostsRecordEachTickOnce()
    {
        var ct = TestContext.Current.CancellationToken;
        await ResetAsync(ct);
        using var first = NewHost(period: 1);
        using var second = NewHost(period: 1);
        first.CreateClient().Dispose();
        second.CreateClient().Dispose();

        await Task.Delay(TimeSpan.FromSeconds(5), ct);
        await first.DisposeAsync();
        await second.DisposeAsync();

        var ticks = await TicksAsync(ct);
        ticks.Count.ShouldBeGreaterThanOrEqualTo(3);
        AssertContiguousAndUnique(ticks, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task ARestartProcessesEveryTickMissedWhileDown()
    {
        var ct = TestContext.Current.CancellationToken;
        await ResetAsync(ct);
        var first = NewHost(period: 1);
        first.CreateClient().Dispose();
        await WaitForAsync(async () => (await TicksAsync(ct)).Count >= 2, ct);
        await first.DisposeAsync();
        var before = (await TicksAsync(ct)).Count;

        await Task.Delay(TimeSpan.FromSeconds(5), ct);
        using var second = NewHost(period: 1);
        second.CreateClient().Dispose();
        await WaitForAsync(async () => (await TicksAsync(ct)).Count >= before + 5, ct);
        await second.DisposeAsync();

        var ticks = await TicksAsync(ct);
        AssertContiguousAndUnique(ticks, TimeSpan.FromSeconds(1));
        ticks.Count.ShouldBeGreaterThanOrEqualTo(before + 5);
    }

    [Fact]
    public async Task AMinuteOfDowntimeIsCaughtUpInOneRun()
    {
        var ct = TestContext.Current.CancellationToken;
        await ResetAsync(ct);
        var last = AlignedNow(TimeSpan.FromSeconds(1)).AddSeconds(-60);
        await SeedAsync(last, ct);
        using var host = NewHost(period: 1);
        host.CreateClient().Dispose();

        await WaitForAsync(async () => (await TicksAsync(ct)).Count >= 60, ct);
        await host.DisposeAsync();

        var ticks = await TicksAsync(ct);
        ticks.Count.ShouldBeGreaterThanOrEqualTo(60);
        ticks[0]["dueAt"].ToUniversalTime().ShouldBe(last.AddSeconds(1));
        AssertContiguousAndUnique(ticks, TimeSpan.FromSeconds(1));
        var firstRun = ticks.Where(tick => tick["processedAt"] == ticks[0]["processedAt"]).ToList();
        firstRun.Count.ShouldBeGreaterThanOrEqualTo(59, "the gap is recorded by the first run, in one update");
    }

    [Fact]
    public async Task ATenSecondPeriodCatchesUpSixTicks()
    {
        var ct = TestContext.Current.CancellationToken;
        await ResetAsync(ct);
        var last = AlignedNow(TimeSpan.FromSeconds(10)).AddSeconds(-60);
        await SeedAsync(last, ct);
        using var host = NewHost(period: 10);
        host.CreateClient().Dispose();

        await WaitForAsync(async () => (await TicksAsync(ct)).Count >= 6, ct);
        await host.DisposeAsync();

        var ticks = await TicksAsync(ct);
        ticks.Take(6).Select(tick => tick["dueAt"].ToUniversalTime()).ShouldBe(Enumerable.Range(1, 6).Select(n => last.AddSeconds(10 * n)));
        AssertContiguousAndUnique(ticks, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task TheFirstRunEverRecordsOnlyTheCurrentTick()
    {
        var ct = TestContext.Current.CancellationToken;
        await ResetAsync(ct);
        using var host = NewHost(period: 60);
        host.CreateClient().Dispose();

        await WaitForAsync(async () => (await TicksAsync(ct)).Count >= 1, ct);
        await host.DisposeAsync();

        (await TicksAsync(ct)).Count.ShouldBe(1);
    }

    private static DateTime AlignedNow(TimeSpan period)
    {
#pragma warning disable RS0030 // a test seeds the state against the real clock
        var ticks = DateTime.UtcNow.Ticks;
#pragma warning restore RS0030
        return new DateTime(ticks - (ticks % period.Ticks), DateTimeKind.Utc);
    }

    private static void AssertContiguousAndUnique(IReadOnlyList<BsonDocument> ticks, TimeSpan period)
    {
        var due = ticks.Select(tick => tick["dueAt"].ToUniversalTime()).ToList();
        due.Distinct().Count().ShouldBe(due.Count, "a tick was recorded twice");
        for (var index = 1; index < due.Count; index++)
        {
            (due[index] - due[index - 1]).ShouldBe(period, "ticks are not contiguous at " + due[index].ToString("O"));
        }
    }

    private static async Task WaitForAsync(Func<Task<bool>> condition, CancellationToken ct)
    {
        var clock = Stopwatch.StartNew();
        while (!await condition())
        {
            clock.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(30), "the condition was not met in time");
            await Task.Delay(50, ct);
        }
    }

    private async Task ResetAsync(CancellationToken ct) =>
        await mongo.Database.GetCollection<BsonDocument>("leases").DeleteManyAsync(_stateFilter, ct);

    private async Task SeedAsync(DateTime lastTickAt, CancellationToken ct) =>
        await mongo.Database.GetCollection<BsonDocument>("leases").InsertOneAsync(
            new BsonDocument { { "_id", "heartbeat:state" }, { "lastTickAt", lastTickAt }, { "recentTicks", new BsonArray() } }, cancellationToken: ct);

    private async Task<List<BsonDocument>> TicksAsync(CancellationToken ct)
    {
        var state = await mongo.Database.GetCollection<BsonDocument>("leases").Find(_stateFilter).FirstOrDefaultAsync(ct);
        return state is null ? [] : state["recentTicks"].AsBsonArray.Select(tick => tick.AsBsonDocument).ToList();
    }

    private SlotBookApiFactory NewHost(int period) => new()
    {
        MongoDatabase = mongo.DatabaseName,
        Settings = new Dictionary<string, string?>
        {
            ["Jobs:LeaseSeconds"] = "3",
            ["Jobs:Heartbeat:Enabled"] = "true",
            ["Jobs:Heartbeat:PeriodSeconds"] = period.ToString(System.Globalization.CultureInfo.InvariantCulture),
        },
    };
}
