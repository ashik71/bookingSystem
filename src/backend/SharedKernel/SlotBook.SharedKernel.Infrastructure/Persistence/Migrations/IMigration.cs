using MongoDB.Driver;

namespace SlotBook.SharedKernel.Infrastructure.Persistence.Migrations;

/// <summary>
/// One schema change. Migrations run at startup in ordinal order of <see cref="Id"/> and follow
/// expand / contract. They must be idempotent: after a lock expiry a migration can run twice.
/// </summary>
public interface IMigration
{
    /// <summary>Unique and ordinal-ordered, for example <c>0001-add-slot-index</c>.</summary>
    public string Id { get; }

    public Task UpAsync(IMongoDatabase database, CancellationToken cancellationToken);
}
