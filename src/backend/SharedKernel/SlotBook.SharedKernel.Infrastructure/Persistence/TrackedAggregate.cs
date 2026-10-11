using SlotBook.SharedKernel.Domain;

namespace SlotBook.SharedKernel.Infrastructure.Persistence;

/// <summary>An aggregate the unit of work has to write: loaded (with its version) or added.</summary>
internal sealed class TrackedAggregate(IAggregateWriter writer, AggregateRoot aggregate, long? loadedVersion, DateTime? createdAt)
{
    public IAggregateWriter Writer { get; } = writer;

    public AggregateRoot Aggregate { get; } = aggregate;

    /// <summary>The version read from the store; <see langword="null"/> for an added aggregate.</summary>
    public long? LoadedVersion { get; } = loadedVersion;

    /// <summary>The stored <c>createdAt</c>, kept so a save doesn't rewrite it.</summary>
    public DateTime? CreatedAt { get; } = createdAt;
}
