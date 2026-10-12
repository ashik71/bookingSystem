#pragma warning disable CA1515, CA1812 // fixture: never used
using MassTransit;

namespace SlotBook.Modules.Fixture.Messaging;

/// <summary>Violation: no definition derived from <c>SlotBookConsumerDefinition</c>.</summary>
public sealed class FixtureOrphanConsumer : IConsumer<FixtureNoteCreated>
{
    public Task Consume(ConsumeContext<FixtureNoteCreated> context) => Task.CompletedTask;
}
