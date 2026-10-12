namespace SlotBook.SharedKernel.Infrastructure.Messaging;

/// <summary>Set on the receive context by <see cref="InboxInnerLogFilter{TConsumer}"/>, read by <see cref="InboxOuterLogFilter{TMessage}"/>.</summary>
internal sealed class InboxReached
{
    public bool Value { get; set; }
}
