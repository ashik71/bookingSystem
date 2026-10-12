using MongoDB.Bson;
using MongoDB.Driver;
using SlotBook.SharedKernel.Infrastructure.Persistence;

namespace SlotBook.SharedKernel.Infrastructure.Messaging;

#pragma warning disable CA1812 // instantiated by dependency injection

/// <summary>
/// Lets startup create a collection MassTransit's MongoDB outbox uses (outbox messages, outbox state, inbox
/// state), so none is created implicitly inside a snapshot transaction. The name is the one MassTransit itself
/// resolves for the document type, read from its registered <see cref="IMongoCollection{TDocument}"/>.
/// The inbox state collection gets a TTL index on <c>Delivered</c> (the date MassTransit sets when a message is
/// delivered), expiring after <see cref="InboxDuplicateDetectionWindow"/>, so the server removes delivered rows and
/// no hosted service touches MongoDB at start. A row whose consume failed has no <c>Delivered</c> and stays.
/// </summary>
internal sealed class BusOutboxCollection(IServiceProvider services, Type collectionServiceType) : IIndexConfiguration
{
    /// <summary>How long MassTransit keeps a delivered inbox row for duplicate detection, and the TTL of that row.</summary>
    internal static readonly TimeSpan InboxDuplicateDetectionWindow = TimeSpan.FromMinutes(30);

    public string CollectionName => ((CollectionNamespace)collectionServiceType.GetProperty(nameof(IMongoCollection<>.CollectionNamespace))!.GetValue(Collection())!).CollectionName;

    public IReadOnlyList<CreateIndexModel<BsonDocument>> Indexes { get; } = collectionServiceType.GenericTypeArguments[0].Name == "InboxState"
        ?
        [
            new(
                Builders<BsonDocument>.IndexKeys.Ascending("Delivered"),
                new CreateIndexOptions { Name = "ix_inbox_states_delivered_ttl", ExpireAfter = InboxDuplicateDetectionWindow }),
        ]
        : [];

    public IReadOnlyCollection<string> RetryableIndexes { get; } = [];

    private object Collection() => services.GetService(collectionServiceType)
        ?? throw new InvalidOperationException($"{collectionServiceType} is not registered.");
}
