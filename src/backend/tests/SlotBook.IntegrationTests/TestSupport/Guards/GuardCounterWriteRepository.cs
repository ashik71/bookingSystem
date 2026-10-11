#pragma warning disable CA1812 // resolved from the container
using SlotBook.SharedKernel.Infrastructure.Persistence;

namespace SlotBook.IntegrationTests.TestSupport.Guards;

internal sealed class GuardCounterWriteRepository(MongoUnitOfWork unitOfWork, TenantCollections collections)
    : MongoWriteRepository<GuardCounter>(unitOfWork, collections, CollectionName)
{
    public const string CollectionName = "guard_counters";
}
