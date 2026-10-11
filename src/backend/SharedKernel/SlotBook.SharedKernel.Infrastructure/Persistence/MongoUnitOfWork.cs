using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using SlotBook.SharedKernel.Application;
using SlotBook.SharedKernel.Contracts;
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
/// the transaction, so a handler that throws writes nothing. <see cref="DisposeAsync"/> aborts without
/// blocking a thread; <see cref="Dispose"/> is the synchronous fallback.
/// </para>
/// </summary>
public sealed class MongoUnitOfWork(IMongoClient client, IClock clock, IServiceProvider services, ILogger<MongoUnitOfWork> logger) : IUnitOfWork, IAsyncDisposable, IDisposable
{
    private static readonly TransactionOptions _transactionOptions = new(
        ReadConcern.Snapshot, ReadPreference.Primary, WriteConcern.WMajority);

    private readonly Dictionary<(string Collection, Guid Id), TrackedAggregate> _tracked = [];
    private readonly List<Func<IPublishEndpoint, CancellationToken, Task>> _events = [];
    private IClientSessionHandle? _session;
    private bool _committed;

    internal static TransactionOptions TransactionOptions => _transactionOptions;

    /// <summary>The session of the running transaction, or <see langword="null"/> before the first use.</summary>
    internal IClientSessionHandle? Session => _session;

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        EnsureOpen();
        if (_tracked.Count == 0 && _events.Count == 0)
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

        if (_events.Count > 0)
        {
            // The scoped publish endpoint is the bus outbox: it writes on this unit of work's session
            // (see UnitOfWorkMongoDbContext), so the events commit or abort with the aggregates.
            var publishEndpoint = services.GetRequiredService<IPublishEndpoint>();
            foreach (var publish in _events)
            {
                await publish(publishEndpoint, cancellationToken).ConfigureAwait(false);
            }
        }

        // Once every write is in, a late cancellation must not abort a commit that may have landed.
        await CommitRetry.RunAsync(
            () => session.CommitTransactionAsync(CancellationToken.None),
            AmbientAttempt.Current?.Budget,
            cancellationToken).ConfigureAwait(false);
    }

    public void AddIntegrationEvent<TEvent>(TEvent integrationEvent)
        where TEvent : TenantEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        EnsureOpen();

        // The closure keeps the concrete type argument, so the event is published as TEvent and not as TenantEvent.
        _events.Add((endpoint, cancellationToken) =>
            endpoint.Publish(integrationEvent, context => context.MessageId = integrationEvent.EventId, cancellationToken));
        logger.LogEventStaged(typeof(TEvent).Name, integrationEvent.EventId);
    }

    public void Dispose() => _session?.Dispose();

    public async ValueTask DisposeAsync()
    {
        var session = _session;
        if (session is null)
        {
            return;
        }

        _session = null;
        try
        {
            if (session.IsInTransaction)
            {
                await session.AbortTransactionAsync().ConfigureAwait(false);
            }
        }
#pragma warning disable CA1031 // The server times the transaction out anyway; cleanup must not mask the handler's outcome.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            logger.LogTransactionAbortFailed(exception);
        }
        finally
        {
            session.Dispose();
        }
    }

    internal async Task<IClientSessionHandle> BeginAsync(CancellationToken cancellationToken)
    {
        EnsureOpen();
        return await EnsureTransactionAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// The session with a running transaction, started on first use and restarted when the last one ended.
    /// Unlike <see cref="BeginAsync"/> it works after <see cref="CommitAsync"/> has started: the outbox
    /// writes the staged events during the commit, and the delivery service reuses one scope for many transactions.
    /// </summary>
    internal async Task<IClientSessionHandle> EnsureTransactionAsync(CancellationToken cancellationToken)
    {
        if (_session is null)
        {
            _session = await client.StartSessionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        if (!_session.IsInTransaction)
        {
            _session.StartTransaction(_transactionOptions);
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
