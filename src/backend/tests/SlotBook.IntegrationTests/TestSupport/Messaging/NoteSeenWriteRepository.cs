#pragma warning disable CA1812 // resolved from the container
using SlotBook.SharedKernel.Infrastructure.Persistence;

namespace SlotBook.IntegrationTests.TestSupport.Messaging;

internal sealed class NoteSeenWriteRepository(MongoUnitOfWork unitOfWork, TenantCollections collections)
    : MongoWriteRepository<NoteSeen>(unitOfWork, collections, CollectionName)
{
    public const string CollectionName = "note_seen";
}
