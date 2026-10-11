using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Shouldly;
using SlotBook.IntegrationTests.TestSupport;
using SlotBook.IntegrationTests.TestSupport.Guards;
using SlotBook.SharedKernel.Application;
using SlotBook.SharedKernel.Application.Dispatching;
using SlotBook.SharedKernel.Domain;
using Xunit;

namespace SlotBook.IntegrationTests.Persistence;

public sealed class ConcurrencyTests(MongoDatabaseFixture mongo) : IClassFixture<MongoDatabaseFixture>, IAsyncLifetime
{
    private const int Writers = 50;
    private const string TenantSlug = "clinic-a";

    private static readonly FilterDefinitionBuilder<BsonDocument> _filter = Builders<BsonDocument>.Filter;

    private SlotBookApiFactory? _factory;

    private SlotBookApiFactory Factory => _factory ?? throw new InvalidOperationException("Not initialized.");

    public async ValueTask InitializeAsync()
    {
        // Collections must exist before they are touched inside a transaction.
        var ct = TestContext.Current.CancellationToken;
        var existing = await (await mongo.Database.ListCollectionNamesAsync(cancellationToken: ct)).ToListAsync(ct);
        if (!existing.Contains(GuardCounterWriteRepository.CollectionName, StringComparer.Ordinal))
        {
            await mongo.Database.CreateCollectionAsync(GuardCounterWriteRepository.CollectionName, cancellationToken: ct);
        }

        _factory = new SlotBookApiFactory { TenantSlug = TenantSlug, MongoDatabase = mongo.DatabaseName };
    }

    public async ValueTask DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }
    }

    [Fact]
    public async Task FiftyParallelIncrementsOfOneGuardAllSucceedAndCountFifty()
    {
        var ct = TestContext.Current.CancellationToken;
        const string key = "parallel";
        using var client = Factory.CreateClient();
        (await IncrementAsync(client, key, ct)).StatusCode.ShouldBe(HttpStatusCode.OK, "creates the guard");

        var statuses = await ConcurrentIncrementsAsync(client, key, Writers, ct);

        statuses.ShouldAllBe(status => status == HttpStatusCode.OK, "no writer may see a 409");
        var document = await GuardsWithIdAsync(key, ct);
        document.Count.ShouldBe(1);
        document[0]["count"].AsInt32.ShouldBe(Writers + 1);
        document[0]["version"].AsInt64.ShouldBe(Writers + 1);
    }

    [Fact]
    public async Task FirstUseRaceOnAKeyedGuardCreatesOneDocument()
    {
        var ct = TestContext.Current.CancellationToken;
        const string key = "first-use";
        using var client = Factory.CreateClient();

        var statuses = await ConcurrentIncrementsAsync(client, key, Writers, ct);

        statuses.ShouldAllBe(status => status == HttpStatusCode.OK, "no writer may see a 409");
        var documents = await GuardsWithIdAsync(key, ct);
        documents.Count.ShouldBe(1, "all first writers collided on one _id");
        documents[0]["count"].AsInt32.ShouldBe(Writers);
        documents[0]["version"].AsInt64.ShouldBe(Writers);
    }

    [Fact]
    public async Task ACommandThatKeepsConflictingAnswers409ConcurrencyConflict()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = Factory.CreateClient();

        var response = await client.PostAsync(new Uri("test-only/counters/always-conflicts/conflict", UriKind.Relative), content: null, ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        problem.GetProperty("code").GetString().ShouldBe(CommonErrors.ConcurrencyConflict);
    }

    [Fact]
    public async Task ARefusalAfterAConflictIsReturnedNotA409()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = Factory.CreateClient();

        var response = await client.PostAsync(new Uri("test-only/counters/conflict-then-refuse/conflict-then-refuse", UriKind.Relative), content: null, ct);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        problem.GetProperty("code").GetString().ShouldBe("counter_full");
    }

    private static Task<HttpResponseMessage> IncrementAsync(HttpClient client, string key, CancellationToken ct) =>
        client.PostAsync(new Uri($"test-only/counters/{key}/increment", UriKind.Relative), content: null, ct);

    private static async Task<HttpStatusCode[]> ConcurrentIncrementsAsync(HttpClient client, string key, int writers, CancellationToken ct)
    {
        using var start = new ManualResetEventSlim(false);
        var tasks = Enumerable.Range(0, writers).Select(_ => Task.Run(
            async () =>
            {
                start.Wait(ct);
                using var response = await IncrementAsync(client, key, ct);
                return response.StatusCode;
            },
            ct)).ToArray();
        await Task.Delay(100, ct);
        start.Set();
        return await Task.WhenAll(tasks);
    }

    private async Task<List<BsonDocument>> GuardsWithIdAsync(string key, CancellationToken ct)
    {
        var id = DeterministicId.ForGuard(DeterministicId.ForTenant(TenantSlug), "counter", key);
        return await mongo.Database.GetCollection<BsonDocument>(GuardCounterWriteRepository.CollectionName)
            .Find(_filter.Eq("_id", new BsonBinaryData(id, GuidRepresentation.Standard)))
            .ToListAsync(ct);
    }
}
