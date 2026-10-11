using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace SlotBook.SharedKernel.Infrastructure.Persistence.Migrations;

/// <summary>
/// The expiring lock document <c>leases/migrations</c> (<c>holder</c>, <c>expiresAt</c>). A crashed holder's
/// lock stops blocking once it expires. Story 7's job runner extends this shape.
/// </summary>
internal sealed class MigrationLock(IMongoDatabase database, IOptions<MongoOptions> options, TimeProvider time)
{
    internal const string CollectionName = "leases";
    internal const string LockId = "migrations";

    private static readonly FilterDefinitionBuilder<BsonDocument> _filter = Builders<BsonDocument>.Filter;

    private IMongoCollection<BsonDocument> Leases => database.GetCollection<BsonDocument>(CollectionName);

    /// <summary>Takes the lock when it is absent or expired. Returns <see langword="null"/> when another holder has it.</summary>
    public async Task<Handle?> TryAcquireAsync(CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var holder = Convert.ToHexStringLower(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));
        var expiresAt = now.AddSeconds(options.Value.MigrationLockExpirySeconds);
        var update = Builders<BsonDocument>.Update
            .Set("holder", holder)
            .Set("expiresAt", expiresAt);
        try
        {
            // A live lock fails the filter, so the upsert tries to insert the same _id and hits DuplicateKey.
            await Leases.UpdateOneAsync(
                _filter.Eq("_id", LockId) & _filter.Lt("expiresAt", now),
                update,
                new UpdateOptions { IsUpsert = true },
                cancellationToken).ConfigureAwait(false);
            return new Handle(Leases, holder);
        }
        catch (MongoWriteException exception) when (exception.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return null;
        }
        catch (MongoCommandException exception) when (exception.Code == 11000)
        {
            return null;
        }
    }

    internal sealed class Handle(IMongoCollection<BsonDocument> leases, string holder)
    {
        /// <summary>Releases the lock unless it expired and someone else took it.</summary>
        public async Task ReleaseAsync()
        {
            await leases.DeleteOneAsync(
                _filter.Eq("_id", LockId) & _filter.Eq("holder", holder)).ConfigureAwait(false);
        }
    }
}
