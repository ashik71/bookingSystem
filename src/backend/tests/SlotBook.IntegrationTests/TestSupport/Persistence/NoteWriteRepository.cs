#pragma warning disable CA1812 // resolved from the container or used as a type argument
using MongoDB.Bson;
using MongoDB.Driver;
using SlotBook.SharedKernel.Infrastructure.Persistence;

namespace SlotBook.IntegrationTests.TestSupport.Persistence;

internal sealed class NoteWriteRepository(MongoUnitOfWork unitOfWork, TenantCollections collections)
    : MongoWriteRepository<Note>(unitOfWork, collections, CollectionName)
{
    public const string CollectionName = "notes";

    public Task<IReadOnlyList<Note>> WithTextAsync(string text, CancellationToken cancellationToken) =>
        FindAsync(Builders<BsonDocument>.Filter.Eq("text", text), cancellationToken);
}
