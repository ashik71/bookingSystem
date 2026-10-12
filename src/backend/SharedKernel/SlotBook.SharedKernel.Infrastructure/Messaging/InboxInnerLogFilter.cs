using MassTransit;

namespace SlotBook.SharedKernel.Infrastructure.Messaging;

/// <summary>Sits inside the outbox filter: it runs only when the message is new to the inbox.</summary>
internal sealed class InboxInnerLogFilter<TConsumer> : IFilter<ConsumerConsumeContext<TConsumer>>
    where TConsumer : class
{
    public Task Send(ConsumerConsumeContext<TConsumer> context, IPipe<ConsumerConsumeContext<TConsumer>> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        context.ReceiveContext.GetOrAddPayload(() => new InboxReached()).Value = true;
        return next.Send(context);
    }

    public void Probe(ProbeContext context) => context.CreateFilterScope("inboxReached");
}
