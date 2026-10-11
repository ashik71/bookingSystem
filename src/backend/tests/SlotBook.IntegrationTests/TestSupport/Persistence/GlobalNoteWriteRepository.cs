#pragma warning disable CA1812 // resolved from the container or used as a type argument
using SlotBook.SharedKernel.Infrastructure.Persistence;

namespace SlotBook.IntegrationTests.TestSupport.Persistence;

internal sealed class GlobalNoteWriteRepository(MongoUnitOfWork unitOfWork, TenantCollections collections)
    : MongoWriteRepository<GlobalNote>(unitOfWork, collections, GlobalNote.CollectionName);
