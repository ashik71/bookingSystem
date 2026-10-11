using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace SlotBook.SharedKernel.Infrastructure.Persistence.Migrations;

/// <summary>
/// Runs the migrations not yet recorded in <c>migrations</c>, in ordinal order of id, under the
/// <see cref="MigrationLock"/>. An instance that finds the lock held polls until it is released or
/// expires, then re-reads <c>migrations</c>.
/// </summary>
internal sealed class MigrationRunner(
    IMongoDatabase database,
    IEnumerable<IMigration> migrations,
    MigrationLock migrationLock,
    TimeProvider time,
    ILogger<MigrationRunner> logger)
{
    internal const string CollectionName = "migrations";

    private static readonly TimeSpan _pollInterval = TimeSpan.FromMilliseconds(250);

    /// <returns>The number of migrations this instance applied.</returns>
    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var ordered = migrations.OrderBy(migration => migration.Id, StringComparer.Ordinal).ToList();
        var duplicate = ordered.GroupBy(migration => migration.Id, StringComparer.Ordinal).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Migration id '{duplicate.Key}' is used by more than one migration.");
        }

        var recorded = database.GetCollection<BsonDocument>(CollectionName);
        if (ordered.Count == 0)
        {
            return 0;
        }

        MigrationLock.Handle? handle;
        var announced = false;
        while ((handle = await migrationLock.TryAcquireAsync(cancellationToken).ConfigureAwait(false)) is null)
        {
            if (!announced)
            {
                logger.LogMigrationLockWaiting();
                announced = true;
            }

            await Task.Delay(_pollInterval, time, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            var done = (await recorded.Find(FilterDefinition<BsonDocument>.Empty).Project(Builders<BsonDocument>.Projection.Include("_id")).ToListAsync(cancellationToken).ConfigureAwait(false))
                .Select(document => document["_id"].AsString)
                .ToHashSet(StringComparer.Ordinal);
            var applied = 0;
            foreach (var migration in ordered.Where(migration => !done.Contains(migration.Id)))
            {
                await migration.UpAsync(database, cancellationToken).ConfigureAwait(false);
                await recorded.ReplaceOneAsync(
                    Builders<BsonDocument>.Filter.Eq("_id", migration.Id),
                    new BsonDocument { { "_id", migration.Id }, { "appliedAt", time.GetUtcNow().UtcDateTime } },
                    new ReplaceOptions { IsUpsert = true },
                    cancellationToken).ConfigureAwait(false);
                logger.LogMigrationApplied(migration.Id);
                applied++;
            }

            return applied;
        }
        finally
        {
            await handle.ReleaseAsync().ConfigureAwait(false);
        }
    }
}
