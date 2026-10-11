using MongoDB.Bson;
using MongoDB.Driver;
using SlotBook.SharedKernel.Infrastructure.Persistence;

namespace SlotBook.SharedKernel.Infrastructure.Messaging;

#pragma warning disable CA1812 // instantiated by dependency injection

/// <summary>
/// Lets startup create a collection MassTransit's MongoDB outbox uses (outbox messages, outbox state, inbox
/// state), so none is created implicitly inside a snapshot transaction. The name is the one MassTransit itself
/// resolves for the document type, read from its registered <see cref="IMongoCollection{TDocument}"/>.
/// It has no indexes: MassTransit's own cleanup service creates the inbox index when 6b turns it on.
/// </summary>
internal sealed class BusOutboxCollection(IServiceProvider services, Type collectionServiceType) : IIndexConfiguration
{
    public string CollectionName => ((CollectionNamespace)collectionServiceType.GetProperty(nameof(IMongoCollection<>.CollectionNamespace))!.GetValue(Collection())!).CollectionName;

    public IReadOnlyList<CreateIndexModel<BsonDocument>> Indexes { get; } = [];

    public IReadOnlyCollection<string> RetryableIndexes { get; } = [];

    private object Collection() => services.GetService(collectionServiceType)
        ?? throw new InvalidOperationException($"{collectionServiceType} is not registered.");
}
