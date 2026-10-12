using MassTransit.MongoDbIntegration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using SlotBook.SharedKernel.Application;
using SlotBook.SharedKernel.Infrastructure.Persistence;

namespace SlotBook.SharedKernel.Infrastructure.Messaging;

#pragma warning disable CA1812 // instantiated by dependency injection

/// <summary>
/// MassTransit's <see cref="MongoDbContext"/> over the unit of work's session (AD-7, AD-10). The bus
/// outbox writes its messages on <see cref="Session"/>, which is the session of the handler's
/// transaction, so the aggregates and the outbox messages commit or abort together. The transaction
/// keeps the unit of work's options (snapshot reads, majority writes). The delivery service resolves
/// the same service in its own scope; there it starts and ends transactions through the same methods.
/// </summary>
internal sealed class UnitOfWorkMongoDbContext(MongoUnitOfWork unitOfWork, IServiceProvider services, IClock clock) : MongoDbContext
{
    // Null until a session was started, like MassTransit's own context: its delivery service reads
    // with Find before it begins a transaction, and a null session makes that an ordinary read.
    public IClientSessionHandle Session => unitOfWork.Session!;

    public Guid? TransactionId { get; private set; }

    public async Task<IClientSessionHandle> StartSession(CancellationToken cancellationToken) =>
        await unitOfWork.EnsureTransactionAsync(cancellationToken).ConfigureAwait(false);

    public async Task BeginTransaction(CancellationToken cancellationToken)
    {
        unitOfWork.JoinIfOutsideCommit();
        await unitOfWork.EnsureTransactionAsync(cancellationToken).ConfigureAwait(false);
        TransactionId ??= Guid.CreateVersion7(clock.UtcNow);
    }

    public async Task CommitTransaction(CancellationToken cancellationToken)
    {
        var session = unitOfWork.Session ?? throw new InvalidOperationException("No MongoDB transaction is open to commit.");
        await CommitRetry.RunAsync(
            () => session.CommitTransactionAsync(CancellationToken.None),
            AmbientAttempt.Current?.Budget,
            cancellationToken).ConfigureAwait(false);
        TransactionId = null;
    }

    public async Task AbortTransaction(CancellationToken cancellationToken)
    {
        if (unitOfWork.Session is { IsInTransaction: true } session)
        {
            await session.AbortTransactionAsync(cancellationToken).ConfigureAwait(false);
        }

        TransactionId = null;
    }

    public MongoDbCollectionContext<T> GetCollection<T>() =>
        new TransactionMongoDbCollectionContext<T>(this, services.GetRequiredService<IMongoCollection<T>>());

    // The unit of work owns the session and ends it.
    public void Dispose()
    {
    }
}
