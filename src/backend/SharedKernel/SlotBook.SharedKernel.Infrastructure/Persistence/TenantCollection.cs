using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using SlotBook.SharedKernel.Domain;

namespace SlotBook.SharedKernel.Infrastructure.Persistence;

/// <summary>
/// Access to one aggregate collection with the tenant filter applied (AD-5). Together with
/// <see cref="TenantCollections"/> this is the only code that calls
/// <see cref="IMongoDatabase.GetCollection{TDocument}(string, MongoCollectionSettings?)"/>; the architecture
/// tests enforce that. A tenant-owned aggregate is read and written only with
/// <c>tenantId == ITenantContext.TenantId</c> in the filter, and <c>tenantId</c> is stamped on
/// insert. A <see cref="GlobalAggregateAttribute"/> aggregate gets no tenant field and no filter.
/// </summary>
/// <typeparam name="T">The aggregate type.</typeparam>
internal sealed class TenantCollection<T> : IAggregateWriter
    where T : AggregateRoot
{
    internal const string IdField = "_id";
    internal const string TenantIdField = "tenantId";
    internal const string VersionField = "version";
    internal const string CreatedAtField = "createdAt";
    internal const string UpdatedAtField = "updatedAt";

    private readonly IMongoCollection<BsonDocument> _collection;
    private readonly Guid? _tenantId;

    public TenantCollection(IMongoDatabase database, string name, Guid tenantId)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var global = typeof(T).GetCustomAttribute<GlobalAggregateAttribute>() is not null;
        if (!global && !typeof(TenantAggregateRoot).IsAssignableFrom(typeof(T)))
        {
            throw new InvalidOperationException(
                $"{typeof(T).Name} is neither a TenantAggregateRoot nor a [GlobalAggregate] (AD-5).");
        }

        Name = name;
        _tenantId = global ? null : tenantId;
        _collection = database.GetCollection<BsonDocument>(name);
    }

    public string Name { get; }

    /// <summary>Reads the documents matching <paramref name="filter"/>, inside the session's snapshot.</summary>
    public async Task<IReadOnlyList<BsonDocument>> FindAsync(
        IClientSessionHandle session, FilterDefinition<BsonDocument>? filter, CancellationToken cancellationToken)
    {
        var scoped = Scope(filter ?? Builders<BsonDocument>.Filter.Empty);
        return await _collection.Find(session, scoped).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task<IReadOnlyList<BsonDocument>> FindByIdAsync(IClientSessionHandle session, Guid id, CancellationToken cancellationToken) =>
        FindAsync(session, Builders<BsonDocument>.Filter.Eq(IdField, new BsonBinaryData(id, GuidRepresentation.Standard)), cancellationToken);

    public async Task WriteAsync(IClientSessionHandle session, TrackedAggregate tracked, DateTime now, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(tracked);

        var aggregate = (T)tracked.Aggregate;
        var document = aggregate.ToBsonDocument(typeof(T));
        if (_tenantId is { } tenantId)
        {
            document[TenantIdField] = new BsonBinaryData(tenantId, GuidRepresentation.Standard);
        }

        document[UpdatedAtField] = now;

        if (tracked.LoadedVersion is not { } loaded)
        {
            document[VersionField] = 1L;
            document[CreatedAtField] = now;
            await _collection.InsertOneAsync(session, document, cancellationToken: cancellationToken).ConfigureAwait(false);
            return;
        }

        document[VersionField] = loaded + 1;
        document[CreatedAtField] = tracked.CreatedAt ?? now;
        var filter = Builders<BsonDocument>.Filter.And(
            Scope(Builders<BsonDocument>.Filter.Eq(IdField, new BsonBinaryData(aggregate.Id, GuidRepresentation.Standard))),
            Builders<BsonDocument>.Filter.Eq(VersionField, loaded));
        var result = await _collection.ReplaceOneAsync(session, filter, document, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (result.MatchedCount == 0)
        {
            throw new VersionConflictException(Name, aggregate.Id);
        }
    }

    internal static T Deserialize(BsonDocument document) => BsonSerializer.Deserialize<T>(document);

    private FilterDefinition<BsonDocument> Scope(FilterDefinition<BsonDocument> filter) =>
        _tenantId is { } tenantId
            ? Builders<BsonDocument>.Filter.And(
                Builders<BsonDocument>.Filter.Eq(TenantIdField, new BsonBinaryData(tenantId, GuidRepresentation.Standard)),
                filter)
            : filter;
}
