using MassTransit;

namespace SlotBook.IntegrationTests.TestSupport.Messaging;

/// <summary>
/// A consume filter inside the outbox filter: after the consumer returned and before the inbox commit it
/// can throw once, the way a crash between the handler and the commit would.
/// </summary>
internal sealed class FaultInjection(ConsumerProbe probe) : IFilter<ConsumerConsumeContext<NoteCreatedConsumer>>
{
    public async Task Send(ConsumerConsumeContext<NoteCreatedConsumer> context, IPipe<ConsumerConsumeContext<NoteCreatedConsumer>> next)
    {
        await next.Send(context).ConfigureAwait(false);
        if (context.TryGetMessage<NoteCreated>(out var created) && probe.TakeFault(created.Message.NoteId))
        {
            throw new InvalidOperationException("Injected fault after the handler, before the inbox commit.");
        }
    }

    public void Probe(ProbeContext context) => context.CreateFilterScope("faultInjection");
}
