using System.Security.Cryptography;
using MongoDB.Bson;
using MongoDB.Driver;

namespace SlotBook.SharedKernel.Infrastructure.Persistence.Leases;

/// <summary>
/// Expiring lease documents in <c>leases</c> (<c>_id</c> = the lease name, <c>holder</c>, <c>token</c>, <c>expiresAt</c>, <c>acquiredAt</c>,
/// <c>renewedAt</c>). A crashed holder's lease stops blocking once it expires; a live holder renews it. Every acquisition gets a random token and every write is
/// filtered on it, so a run that lost its lease can neither renew nor release a later acquisition's, even by the same host.
/// <c>holder</c> stays for diagnostics.
/// </summary>
internal sealed class LeaseStore(IMongoDatabase database, LeaseHolder holder, TimeProvider time)
{
    internal const string CollectionName = "leases";

    private static readonly FilterDefinitionBuilder<BsonDocument> _filter = Builders<BsonDocument>.Filter;

    private IMongoCollection<BsonDocument> Leases => database.GetCollection<BsonDocument>(CollectionName);

    /// <summary>Takes the lease when it is absent or expired. Returns <see langword="null"/> when another holder has it.</summary>
    public async Task<Lease?> TryAcquireAsync(string name, TimeSpan duration, CancellationToken cancellationToken)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var expiresAt = now.Add(duration);
        var token = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));
        var update = Builders<BsonDocument>.Update
            .Set("holder", holder.Id)
            .Set("token", token)
            .Set("expiresAt", expiresAt)
            .Set("acquiredAt", now)
            .Set("renewedAt", now);
        try
        {
            // A live lease fails the filter, so the upsert tries to insert the same _id and hits DuplicateKey.
            await Leases.UpdateOneAsync(
                _filter.Eq("_id", name) & _filter.Lt("expiresAt", now),
                update,
                new UpdateOptions { IsUpsert = true },
                cancellationToken).ConfigureAwait(false);
            return new Lease(name, holder.Id, token, expiresAt);
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

    /// <summary>Extends a held lease. Returns <see langword="false"/> when it was lost: expired, or taken by another holder.</summary>
    public async Task<bool> RenewAsync(Lease lease, TimeSpan duration, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(lease);

        var now = time.GetUtcNow().UtcDateTime;
        var expiresAt = now.Add(duration);
        var result = await Leases.UpdateOneAsync(
            _filter.Eq("_id", lease.Name) & _filter.Eq("token", lease.Token) & _filter.Gt("expiresAt", now),
            Builders<BsonDocument>.Update.Set("expiresAt", expiresAt).Set("renewedAt", now),
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (result.MatchedCount == 0)
        {
            return false;
        }

        lease.ExpiresAt = expiresAt;
        return true;
    }

    /// <summary>Releases the lease unless it expired and someone else took it.</summary>
    public async Task ReleaseAsync(Lease lease, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(lease);

        await Leases.DeleteOneAsync(
            _filter.Eq("_id", lease.Name) & _filter.Eq("token", lease.Token),
            cancellationToken).ConfigureAwait(false);
    }
}
