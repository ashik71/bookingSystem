using MongoDB.Driver;

namespace SlotBook.SharedKernel.Infrastructure.Persistence;

/// <summary>Writes one tracked aggregate into its collection inside the unit of work's transaction.</summary>
internal interface IAggregateWriter
{
    public string Name { get; }

    public Task WriteAsync(IClientSessionHandle session, TrackedAggregate tracked, DateTime now, CancellationToken cancellationToken);
}
