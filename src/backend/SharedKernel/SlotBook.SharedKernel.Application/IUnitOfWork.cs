using SlotBook.SharedKernel.Contracts;

namespace SlotBook.SharedKernel.Application;

/// <summary>
/// The write boundary of one command handler (AD-7). Handlers load aggregates through write
/// repositories and call <see cref="CommitAsync"/> once; nothing is written before that.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Saves every aggregate loaded or added through a write repository, and writes every staged
    /// integration event to the outbox, in one transaction: all of them, or none. Call it at most
    /// once per unit of work.
    /// </summary>
    public Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages an integration event (AD-10). It leaves through the outbox when <see cref="CommitAsync"/>
    /// commits, never before, and not at all when the handler fails. The type argument is the concrete
    /// event type; the event is published as that type.
    /// </summary>
    public void AddIntegrationEvent<TEvent>(TEvent integrationEvent)
        where TEvent : TenantEvent;
}
