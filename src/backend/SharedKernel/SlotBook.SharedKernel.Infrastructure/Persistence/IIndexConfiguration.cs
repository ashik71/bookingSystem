using MongoDB.Bson;
using MongoDB.Driver;

namespace SlotBook.SharedKernel.Infrastructure.Persistence;

/// <summary>
/// The indexes of one aggregate's collection (AD-9). Startup creates them after the migrations have run.
/// Index names start with <c>ux_</c> (unique) or <c>ix_</c>; keys of tenant-owned collections lead with <c>tenantId</c>.
/// </summary>
public interface IIndexConfiguration
{
    /// <summary>The collection name, without the database.</summary>
    public string CollectionName { get; }

    /// <summary>The indexes to ensure; creating an existing index with the same definition is a no-op.</summary>
    public IReadOnlyList<CreateIndexModel<BsonDocument>> Indexes { get; }

    /// <summary>Names of unique indexes whose <c>DuplicateKey</c> re-runs the handler (AD-8).</summary>
    public IReadOnlyCollection<string> RetryableIndexes { get; }
}
