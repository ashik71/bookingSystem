namespace SlotBook.SharedKernel.Infrastructure.Persistence;

/// <summary>
/// The unique indexes whose <c>DuplicateKey</c> re-runs the handler (AD-8), as (collection, index)
/// pairs. <c>_id</c> of every collection is always retryable (AD-6 guards). A module's index
/// configuration adds its indexes here.
/// </summary>
public sealed class RetryableDuplicateKeys
{
    internal const string IdIndex = "_id_";

    private readonly HashSet<(string Collection, string Index)> _indexes = [];

    /// <summary>Marks a unique index as retryable.</summary>
    /// <param name="collection">The collection name, without the database.</param>
    /// <param name="indexName">The index name.</param>
    public void Add(string collection, string indexName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(collection);
        ArgumentException.ThrowIfNullOrWhiteSpace(indexName);
        lock (_indexes)
        {
            _indexes.Add((collection, indexName));
        }
    }

    internal bool Contains(string collection, string indexName)
    {
        if (indexName == IdIndex)
        {
            return true;
        }

        lock (_indexes)
        {
            return _indexes.Contains((collection, indexName));
        }
    }
}
