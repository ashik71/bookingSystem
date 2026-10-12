#pragma warning disable CA1515 // public: MassTransit's scan only sees exported types
using MassTransit;
using SlotBook.SharedKernel.Application.Dispatching;

namespace SlotBook.IntegrationTests.TestSupport.Messaging;

/// <summary>A thin adapter (AD-10): turns the event into a command and holds no logic. It only counts its invocations.</summary>
public sealed class NoteCreatedConsumer : IConsumer<NoteCreated>
{
    private readonly IDispatcher _dispatcher;
    private readonly ConsumerProbe _probe;

    public NoteCreatedConsumer(IDispatcher dispatcher, ConsumerProbe probe)
    {
        _dispatcher = dispatcher;
        _probe = probe;
    }

    public async Task Consume(ConsumeContext<NoteCreated> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _probe.NextInvocation(context.Message.NoteId);
        await _dispatcher.SendAsync(new RecordNoteSeenCommand(context.Message.NoteId, _probe.BehaviorFor(context.Message.NoteId)), context.CancellationToken).ConfigureAwait(false);
    }
}
