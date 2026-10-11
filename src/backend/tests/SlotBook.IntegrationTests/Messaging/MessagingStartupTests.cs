using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using SlotBook.IntegrationTests.TestSupport.Messaging;
using SlotBook.SharedKernel.Infrastructure.Messaging;
using Xunit;

namespace SlotBook.IntegrationTests.Messaging;

public sealed class MessagingStartupTests
{
    [Fact]
    public void StartupFailsWhenTheRabbitMqConnectionStringIsMissing()
    {
        using var missing = new SlotBookApiFactory { RabbitMqConnectionString = null };

        var exception = Should.Throw<OptionsValidationException>(() => missing.CreateClient());

        exception.Message.ShouldContain("ConnectionStrings:rabbitmq");
    }

    [Fact]
    public void StartupDoesNotWaitForTheBroker()
    {
        // Nothing listens on port 1: the host starts anyway, and the outbox keeps commits succeeding.
        using var unreachable = new SlotBookApiFactory { RabbitMqConnectionString = "amqp://guest:guest@127.0.0.1:1/" };

        using var client = unreachable.CreateClient();

        client.ShouldNotBeNull();
    }

    [Fact]
    public async Task ReadinessDoesNotDependOnTheBroker()
    {
        using var unreachable = new SlotBookApiFactory { RabbitMqConnectionString = "amqp://guest:guest@127.0.0.1:1/" };
        using var client = unreachable.CreateClient();

        var response = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .ShouldBe("""{"status":"Healthy","checks":{"mongodb":"Healthy"}}""");
    }

    [Fact]
    public void ConsumersOfTheGivenAssembliesAreRegistered()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) { ["ConnectionStrings:rabbitmq"] = "amqp://guest:guest@localhost/" })
            .Build();
        var services = new ServiceCollection();

        services.AddSlotBookMessaging(configuration, typeof(NoteCreatedConsumer).Assembly);

        services.ShouldContain(descriptor => descriptor.ServiceType == typeof(NoteCreatedConsumer));
    }
}
