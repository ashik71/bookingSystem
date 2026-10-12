using MongoDB.Bson;
using MongoDB.Driver;

namespace SlotBook.SharedKernel.Infrastructure.Persistence.Leases;

/// <summary>
/// The heartbeat job's state document in <c>leases</c> (<c>_id</c> = <c>heartbeat:state</c>): the watermark <c>lastTickAt</c> and
/// the capped list <c>recentTicks</c> (<c>dueAt</c>, <c>processedAt</c>, <c>holder</c>). Infrastructure state, not an aggregate.
/// </summary>
internal sealed class HeartbeatStateStore(IMongoDatabase database, LeaseHolder holder)
{
    internal const string StateId = "heartbeat:state";

    internal const int MaxRecentTicks = 1440;

    private static readonly FilterDefinitionBuilder<BsonDocument> _filter = Builders<BsonDocument>.Filter;

    private IMongoCollection<BsonDocument> Leases => database.GetCollection<BsonDocument>(LeaseStore.CollectionName);

    /// <summary>The watermark, or <see langword="null"/> before the first tick was ever recorded.</summary>
    public async Task<DateTime?> ReadLastTickAtAsync(CancellationToken cancellationToken)
    {
        var state = await Leases.Find(_filter.Eq("_id", StateId))
            .Project(Builders<BsonDocument>.Projection.Include("lastTickAt"))
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return state is not null && state.TryGetValue("lastTickAt", out var value) ? value.ToUniversalTime() : null;
    }

    /// <summary>
    /// Records <paramref name="dueTicks"/> and moves the watermark to the last one, in one update that only applies while the
    /// watermark is still <paramref name="expectedLastTickAt"/>. Returns <see langword="false"/> when another run got there first.
    /// </summary>
    public async Task<bool> TryRecordAsync(
        DateTime? expectedLastTickAt, IReadOnlyList<DateTime> dueTicks, DateTime processedAt, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dueTicks);
        if (dueTicks.Count == 0)
        {
            return true;
        }

        var ticks = dueTicks.Select(due => new BsonDocument { { "dueAt", due }, { "processedAt", processedAt }, { "holder", holder.Id } });
        var update = Builders<BsonDocument>.Update
            .Set("lastTickAt", dueTicks[^1])
            .PushEach("recentTicks", ticks, slice: -MaxRecentTicks);
        var match = expectedLastTickAt is null
            ? _filter.Eq("_id", StateId) & _filter.Exists("lastTickAt", false)
            : _filter.Eq("_id", StateId) & _filter.Eq("lastTickAt", expectedLastTickAt.Value);
        try
        {
            var result = await Leases.UpdateOneAsync(match, update, new UpdateOptions { IsUpsert = expectedLastTickAt is null }, cancellationToken).ConfigureAwait(false);
            return result.MatchedCount > 0 || result.UpsertedId is not null;
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
        catch (MongoCommandException exception) when (exception.Code == 11000)
        {
            return false;
        }
    }
}
