namespace SlotBook.SharedKernel.Application;

/// <summary>
/// The write boundary of one command handler (AD-7). Handlers load aggregates through write
/// repositories and call <see cref="CommitAsync"/> once; nothing is written before that.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Saves every aggregate loaded or added through a write repository in one transaction: all
    /// of them are written, or none. Call it at most once per unit of work.
    /// </summary>
    public Task CommitAsync(CancellationToken cancellationToken = default);
}
