#pragma warning disable CA1515 // public: used as an xunit fixture type
using RabbitMQ.Client;
using Xunit;

namespace SlotBook.IntegrationTests.TestSupport;

/// <summary>A RabbitMQ connection for the test class. Tests declare their own exclusive queues.</summary>
public sealed class RabbitMqFixture : IAsyncLifetime
{
    private IConnection? _connection;

    public IConnection Connection => _connection ?? throw new InvalidOperationException("Fixture not initialised.");

    public async ValueTask InitializeAsync()
    {
        var factory = new ConnectionFactory { Uri = new Uri(IntegrationEnvironment.RabbitMq) };
        _connection = await factory.CreateConnectionAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }
    }
}
