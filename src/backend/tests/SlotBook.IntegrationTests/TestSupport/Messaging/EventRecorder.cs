using System.Collections.Concurrent;
using MassTransit;
using SlotBook.SharedKernel.Contracts;

namespace SlotBook.IntegrationTests.TestSupport.Messaging;

/// <summary>
/// A plain consumer without inbox: a temporary receive endpoint on the host's bus that records every
/// event of the given types that <c>accept</c> admits, with the message id the bus delivered it under.
/// The broker has one exchange per event type, shared by every test host, hence the filter.
/// </summary>
internal sealed class EventRecorder(Func<TenantEvent, bool> accept) : IAsyncDisposable
{
    private readonly ConcurrentQueue<Received> _received = new();
    private HostReceiveEndpointHandle? _handle;

    public IReadOnlyCollection<Received> All => _received.ToArray();

    public static async Task<EventRecorder> StartAsync(IBus bus, string endpointName, IEnumerable<Type> eventTypes, Func<TenantEvent, bool> accept, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(eventTypes);
        ArgumentNullException.ThrowIfNull(accept);

        var recorder = new EventRecorder(accept);
        var types = eventTypes.ToList();
        recorder._handle = bus.ConnectReceiveEndpoint(endpointName, endpoint =>
        {
            if (endpoint is IRabbitMqReceiveEndpointConfigurator rabbit)
            {
                // Test queues vanish with their connection.
                rabbit.AutoDelete = true;
                rabbit.Durable = false;
            }

            foreach (var type in types)
            {
                typeof(EventRecorder).GetMethod(nameof(Record), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .MakeGenericMethod(type)
                    .Invoke(recorder, [endpoint]);
            }
        });
        await recorder._handle.Ready.WaitAsync(cancellationToken).ConfigureAwait(false);
        return recorder;
    }

    public async Task<IReadOnlyList<Received>> WaitForAsync(int count, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limit.CancelAfter(timeout);
        try
        {
            while (_received.Count < count)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50), limit.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Expected {count} events within {timeout}, received {_received.Count}.", exception);
        }

        return _received.ToArray();
    }

    public async ValueTask DisposeAsync()
    {
        if (_handle is not null)
        {
            await _handle.StopAsync().ConfigureAwait(false);
        }
    }

    private void Record<T>(IReceiveEndpointConfigurator endpoint)
        where T : TenantEvent =>
        endpoint.Handler<T>(context =>
        {
            if (accept(context.Message))
            {
                _received.Enqueue(new Received(context.Message, context.MessageId));
            }

            return Task.CompletedTask;
        });

    internal sealed record Received(object Message, Guid? MessageId);
}
