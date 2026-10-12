#pragma warning disable CA1515 // public: MassTransit's scan only sees exported types
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using SlotBook.SharedKernel.Infrastructure.Messaging;

namespace SlotBook.IntegrationTests.TestSupport.Messaging;

/// <summary>The definition of the test consumer, with the fault filter inside the outbox filter.</summary>
public sealed class NoteCreatedConsumerDefinition : SlotBookConsumerDefinition<NoteCreatedConsumer>
{
    public NoteCreatedConsumerDefinition(EndpointNaming naming)
        : base(naming, "notes", "record-note-seen")
    {
    }

    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<NoteCreatedConsumer> consumerConfigurator, IRegistrationContext context)
    {
        ArgumentNullException.ThrowIfNull(consumerConfigurator);
        ArgumentNullException.ThrowIfNull(context);

        base.ConfigureConsumer(endpointConfigurator, consumerConfigurator, context);
        consumerConfigurator.UseFilter(new FaultInjection(context.GetRequiredService<ConsumerProbe>()));
    }
}
