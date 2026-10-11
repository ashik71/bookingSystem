using MongoDB.Bson;
using MongoDB.Driver;
using SlotBook.SharedKernel.Domain;

namespace SlotBook.SharedKernel.Infrastructure.Persistence;

/// <summary>
/// Base of every <c>I{Aggregate}WriteRepository</c> (AD-7). Loads go through the tenant filter and
/// register the aggregate with the unit of work, which writes it on commit (see
/// <see cref="MongoUnitOfWork"/>); handlers never touch a collection. An aggregate type needs a
/// private parameterless constructor so the driver can rebuild it; its state is set through the
/// private setters.
/// </summary>
/// <typeparam name="T">The aggregate type.</typeparam>
public abstract class MongoWriteRepository<T>
    where T : AggregateRoot
{
    private readonly MongoUnitOfWork _unitOfWork;
    private readonly TenantCollection<T> _collection;

    protected MongoWriteRepository(MongoUnitOfWork unitOfWork, TenantCollections collections, string collectionName)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(collections);

        _unitOfWork = unitOfWork;
        _collection = collections.For<T>(collectionName);
    }

    /// <summary>
    /// Loads the aggregate, or <see langword="null"/> when it doesn't exist for this tenant.
    /// Every aggregate returned is written on commit (its version is bumped); use it only for
    /// aggregates the handler writes, and use a read repository or a query for reads.
    /// </summary>
    public async Task<T?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (_unitOfWork.Tracked<T>(_collection.Name, id) is { } tracked)
        {
            return tracked;
        }

        var session = await _unitOfWork.BeginAsync(cancellationToken).ConfigureAwait(false);
        var documents = await _collection.FindByIdAsync(session, id, cancellationToken).ConfigureAwait(false);
        return documents.Count == 0 ? null : _unitOfWork.Load(_collection, documents[0]);
    }

    /// <summary>Stages a new aggregate; it is inserted on commit.</summary>
    public void Add(T aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        _unitOfWork.Add(_collection, aggregate);
    }

    /// <summary>
    /// Loads the tenant's aggregates matching <paramref name="filter"/>, for the repository's own queries.
    /// Every aggregate returned is written on commit (its version is bumped, one write per document);
    /// use it only for aggregates the handler writes, and use a read repository or a query for lists.
    /// </summary>
    protected async Task<IReadOnlyList<T>> FindAsync(FilterDefinition<BsonDocument> filter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var session = await _unitOfWork.BeginAsync(cancellationToken).ConfigureAwait(false);
        var documents = await _collection.FindAsync(session, filter, cancellationToken).ConfigureAwait(false);
        return [.. documents.Select(document => _unitOfWork.Load(_collection, document))];
    }
}
