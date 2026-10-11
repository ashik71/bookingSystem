using MongoDB.Bson;
using MongoDB.Driver;

namespace SlotBook.IntegrationTests.TestSupport.Messaging;

/// <summary>
/// Waits until the bus outbox has handed every message to the broker, so a test can assert on what
/// arrived and a host is not disposed with messages in flight (AD-19).
/// </summary>
internal static class OutboxDrain
{
    public const string MessageCollection = "outbox.messages";

    public const string StateCollection = "outbox.states";

    public static async Task WaitAsync(IMongoDatabase database, TimeSpan timeout, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(database);

        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limit.CancelAfter(timeout);
        try
        {
            while (await CountAsync(database, limit.Token).ConfigureAwait(false) > 0)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), limit.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"The outbox still held {await CountAsync(database, cancellationToken).ConfigureAwait(false)} undelivered messages after {timeout}.", exception);
        }
    }

    public static async Task<long> CountAsync(IMongoDatabase database, CancellationToken cancellationToken) =>
        await database.GetCollection<BsonDocument>(MessageCollection).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: cancellationToken).ConfigureAwait(false);
}
