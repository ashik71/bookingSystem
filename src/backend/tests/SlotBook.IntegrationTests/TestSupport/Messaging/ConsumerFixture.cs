using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using RabbitMQ.Client;
using SlotBook.SharedKernel.Application;
using SlotBook.SharedKernel.Domain;
using Xunit;

namespace SlotBook.IntegrationTests.TestSupport.Messaging;

/// <summary>
/// What the consume tests share: a <see cref="ConsumerHost"/> on the class's database, and helpers to
/// publish an event the way an outside publisher would, to read the counter, the inbox and the error queue.
/// </summary>
internal sealed class ConsumerFixture(MongoDatabaseFixture mongo, string tenantSlug) : IAsyncDisposable
{
    public static readonly TimeSpan Wait = TimeSpan.FromSeconds(30);

    private ConsumerHost? _host;

    public ConsumerHost Host => _host ?? throw new InvalidOperationException("Not started.");

    public Guid TenantId => DeterministicId.ForTenant(tenantSlug);

    public static Guid Id(string test, string part) => DeterministicId.From(DeterministicId.SlotBookNamespace, $"consumer-test:{test}:{part}");

    public static async Task WaitUntilAsync(Func<Task<bool>> condition, string what, CancellationToken cancellationToken)
    {
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limit.CancelAfter(Wait);
        try
        {
            while (!await condition())
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), limit.Token);
            }
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Waited {Wait} for {what}.", exception);
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Collections must exist before they are touched inside a transaction.
        var existing = await (await mongo.Database.ListCollectionNamesAsync(cancellationToken: cancellationToken)).ToListAsync(cancellationToken);
        foreach (var name in new[] { NoteSeenWriteRepository.CollectionName, Persistence.NoteWriteRepository.CollectionName })
        {
            if (!existing.Contains(name, StringComparer.Ordinal))
            {
                await mongo.Database.CreateCollectionAsync(name, cancellationToken: cancellationToken);
            }
        }

        _host = await ConsumerHost.StartAsync(mongo.DatabaseName, tenantSlug, cancellationToken);
    }

    /// <summary>Publishes straight to the bus (no outbox) under the given message id, like a broker redelivery would repeat it.</summary>
    public async Task PublishAsync(Guid noteId, Guid messageId, CancellationToken cancellationToken)
    {
        var stamp = EventStamp.Now(Host.Host.Services.GetRequiredService<IClock>());
        await Host.Host.Services.GetRequiredService<IBus>().Publish(
            new NoteCreated { EventId = messageId, TenantId = TenantId, OccurredAt = stamp.OccurredAt, NoteId = noteId, Text = "inbox" },
            context => context.MessageId = messageId,
            cancellationToken);
    }

    public async Task<int> CounterAsync(Guid noteId, CancellationToken cancellationToken)
    {
        var id = DeterministicId.ForGuard(TenantId, "note-seen", noteId.ToString());
        var document = await mongo.Database.GetCollection<BsonDocument>(NoteSeenWriteRepository.CollectionName)
            .Find(Builders<BsonDocument>.Filter.Eq("_id", new BsonBinaryData(id, GuidRepresentation.Standard)))
            .FirstOrDefaultAsync(cancellationToken);
        return document?["count"].ToInt32() ?? 0;
    }

    public async Task<long> InboxRecordsAsync(Guid messageId, CancellationToken cancellationToken) =>
        await mongo.Database.GetCollection<BsonDocument>("inbox.states")
            .CountDocumentsAsync(Builders<BsonDocument>.Filter.Eq("MessageId", new BsonBinaryData(messageId, GuidRepresentation.Standard)), cancellationToken: cancellationToken);

    /// <summary>The ids of the messages in the error queue of the consumer endpoint, read without leaving them there.</summary>
    public async Task<IReadOnlyList<string>> ErrorQueueMessageIdsAsync(IConnection connection, CancellationToken cancellationToken)
    {
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
        var ids = new List<string>();
        try
        {
            while (await channel.BasicGetAsync(Host.Endpoint + "_error", autoAck: true, cancellationToken) is { } message)
            {
                ids.Add(message.BasicProperties.MessageId ?? string.Empty);
            }
        }
        catch (RabbitMQ.Client.Exceptions.OperationInterruptedException)
        {
            // No error queue yet: nothing failed.
        }

        return ids;
    }

    public async ValueTask DisposeAsync()
    {
        if (_host is null)
        {
            return;
        }

        var endpoint = Host.Endpoint;
        await OutboxDrain.WaitAsync(mongo.Database, Wait, CancellationToken.None);
        await _host.DisposeAsync();
        var factory = new ConnectionFactory { Uri = new Uri(IntegrationEnvironment.RabbitMq) };
        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.QueueDeleteAsync(endpoint);
        await channel.QueueDeleteAsync(endpoint + "_error");
    }
}
