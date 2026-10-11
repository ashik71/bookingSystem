#pragma warning disable CA1515 // public: MassTransit's scan only sees exported types
using MassTransit;

namespace SlotBook.IntegrationTests.TestSupport.Messaging;

/// <summary>A consumer for the scan test. The other tests record events with <see cref="EventRecorder"/>.</summary>
public sealed class NoteCreatedConsumer : IConsumer<NoteCreated>
{
    public Task Consume(ConsumeContext<NoteCreated> context) => Task.CompletedTask;
}
