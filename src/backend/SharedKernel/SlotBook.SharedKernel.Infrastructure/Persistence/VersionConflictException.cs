namespace SlotBook.SharedKernel.Infrastructure.Persistence;

/// <summary>
/// A version-checked save matched no document: the aggregate changed after it was loaded (AD-7).
/// The command dispatcher re-runs the handler on it (story 5b).
/// </summary>
public sealed class VersionConflictException : Exception
{
    public VersionConflictException(string collection, Guid id)
        : base($"Version conflict saving {id} in '{collection}'.")
    {
        Collection = collection;
        AggregateId = id;
    }

    public VersionConflictException()
        : this("unknown", Guid.Empty)
    {
    }

    public VersionConflictException(string message)
        : base(message)
    {
        Collection = "unknown";
    }

    public VersionConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
        Collection = "unknown";
    }

    public string Collection { get; }

    public Guid AggregateId { get; }
}
