using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using SlotBook.SharedKernel.Application;
using SlotBook.SharedKernel.Domain;

namespace SlotBook.SharedKernel.Infrastructure.Persistence;

/// <summary>
/// The MongoDB unit of work (AD-7): one per command attempt. The first load or the commit starts a
/// transaction with read concern <c>snapshot</c> and write concern <c>majority</c>, so every load is
/// inside the snapshot and a concurrent change shows up as a write conflict.
/// <para>
/// <b>Every aggregate loaded through a write repository is written on commit, changed or not</b>
/// (its <c>version</c> is incremented). AD-7 speaks of "each changed aggregate"; the rule here is
/// stricter on purpose: loading through a write repository is the declaration that the handler takes
/// part in the write, which is what makes "write the guard" (AD-6) impossible to forget. Read-only
/// access goes through queries or read repositories. Added aggregates are inserted with version 1.
/// </para>
/// <para>
/// The save of a loaded aggregate has <c>{ _id, tenantId, version }</c> in its filter and throws
/// <see cref="VersionConflictException"/> when it matches nothing. Disposing without a commit aborts
/// the transaction, so a handler that throws writes nothing.
/// </para>
/// </summary>
public sealed class MongoUnitOfWork(IMongoClient client, IClock clock) : IUnitOfWork, IDisposable
{
    private static readonly TransactionOptions _transactionOptions = new(
        ReadConcern.Snapshot, ReadPreference.Primary, WriteConcern.WMajority);

    private readonly Dictionary<(string Collection, Guid Id), TrackedAggregate> _tracked = [];
    private IClientSessionHandle? _session;
    private bool _committed;

    internal static TransactionOptions TransactionOptions => _transactionOptions;

    /// <summary>The session of the running transaction, or <see langword="null"/> before the first use.</summary>
    internal IClientSessionHandle? Session => _session;

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        EnsureOpen();
        if (_tracked.Count == 0)
        {
            _committed = true;
            return;
        }

        var session = await BeginAsync(cancellationToken).ConfigureAwait(false);
        _committed = true;
        var now = clock.UtcNow.UtcDateTime;
        foreach (var tracked in _tracked.Values)
        {
            await tracked.Writer.WriteAsync(session, tracked, now, cancellationToken).ConfigureAwait(false);
        }

        await session.CommitTransactionAsync(cancellationToken).ConfigureAwait(false);
    }

    public void Dispose() => _session?.Dispose();

    internal async Task<IClientSessionHandle> BeginAsync(CancellationToken cancellationToken)
    {
        EnsureOpen();
        if (_session is null)
        {
            var session = await client.StartSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            session.StartTransaction(_transactionOptions);
            _session = session;
        }

        return _session;
    }

    internal T? Tracked<T>(string collection, Guid id)
        where T : AggregateRoot
    {
        EnsureOpen();
        return _tracked.TryGetValue((collection, id), out var tracked) ? (T)tracked.Aggregate : null;
    }

    /// <summary>Tracks a loaded aggregate; a second load of the same id returns the first instance.</summary>
    internal T Load<T>(TenantCollection<T> collection, BsonDocument document)
        where T : AggregateRoot
    {
        var aggregate = TenantCollection<T>.Deserialize(document);
        if (Tracked<T>(collection.Name, aggregate.Id) is { } existing)
        {
            return existing;
        }

        var version = document.GetValue(TenantCollection<T>.VersionField, 0L).ToInt64();
        DateTime? createdAt = document.TryGetValue(TenantCollection<T>.CreatedAtField, out var created) && created.IsValidDateTime
            ? created.ToUniversalTime()
            : null;
        _tracked[(collection.Name, aggregate.Id)] = new TrackedAggregate(collection, aggregate, version, createdAt);
        return aggregate;
    }

    internal void Add<T>(TenantCollection<T> collection, T aggregate)
        where T : AggregateRoot
    {
        EnsureOpen();
        if (!_tracked.TryAdd((collection.Name, aggregate.Id), new TrackedAggregate(collection, aggregate, null, null)))
        {
            throw new InvalidOperationException($"{typeof(T).Name} {aggregate.Id} is already part of this unit of work.");
        }
    }

    private void EnsureOpen()
    {
        if (_committed)
        {
            throw new InvalidOperationException("The unit of work was already committed; start a new one.");
        }
    }
}
