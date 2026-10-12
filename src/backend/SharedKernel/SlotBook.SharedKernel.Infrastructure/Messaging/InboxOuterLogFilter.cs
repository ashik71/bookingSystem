using MassTransit;
using Microsoft.Extensions.Logging;

namespace SlotBook.SharedKernel.Infrastructure.Messaging;

#pragma warning disable CA1812 // instantiated by MassTransit

/// <summary>
/// Sits outside the outbox filter. When the consume ends without the inner <see cref="InboxInnerLogFilter{TConsumer}"/>
/// having run, the inbox dropped the message as a duplicate.
/// </summary>
internal sealed class InboxOuterLogFilter<TMessage>(ILogger<InboxOuterLogFilter<TMessage>> logger) : IFilter<ConsumeContext<TMessage>>
    where TMessage : class
{
    public async Task Send(ConsumeContext<TMessage> context, IPipe<ConsumeContext<TMessage>> next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var reached = context.ReceiveContext.GetOrAddPayload(() => new InboxReached());
        reached.Value = false;
        await next.Send(context).ConfigureAwait(false);
        if (!reached.Value)
        {
            var endpoint = context.ReceiveContext.InputAddress?.AbsolutePath.TrimStart('/') ?? typeof(TMessage).Name;
            var messageId = context.MessageId;
            logger.LogConsumeDeduplicated(messageId, endpoint);
        }
    }

    public void Probe(ProbeContext context) => context.CreateFilterScope("inboxLog");
}
