using Microsoft.Extensions.Options;
using SlotBook.SharedKernel.Infrastructure.Persistence.Leases;

namespace SlotBook.SharedKernel.Infrastructure.Persistence.Migrations;

/// <summary>
/// The lease <c>leases/migrations</c>, taken through the <see cref="LeaseStore"/>. A crashed holder's
/// lock stops blocking once it expires.
/// </summary>
internal sealed class MigrationLock(LeaseStore leases, IOptions<MongoOptions> options)
{
    internal const string CollectionName = LeaseStore.CollectionName;
    internal const string LockId = "migrations";

    /// <summary>Takes the lock when it is absent or expired. Returns <see langword="null"/> when another holder has it.</summary>
    public async Task<Handle?> TryAcquireAsync(CancellationToken cancellationToken)
    {
        var lease = await leases.TryAcquireAsync(LockId, TimeSpan.FromSeconds(options.Value.MigrationLockExpirySeconds), cancellationToken).ConfigureAwait(false);
        return lease is null ? null : new Handle(leases, lease);
    }

    internal sealed class Handle(LeaseStore leases, Lease lease)
    {
        /// <summary>Releases the lock unless it expired and someone else took it.</summary>
        public async Task ReleaseAsync()
        {
            await leases.ReleaseAsync(lease, CancellationToken.None).ConfigureAwait(false);
        }
    }
}
