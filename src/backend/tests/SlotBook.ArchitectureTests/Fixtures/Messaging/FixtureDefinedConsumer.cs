#pragma warning disable CA1515, CA1812 // fixture: never used
using MassTransit;

namespace SlotBook.Modules.Fixture.Messaging;

/// <summary>Not a violation: has a definition.</summary>
public sealed class FixtureDefinedConsumer : IConsumer<FixtureNoteCreated>
{
    public Task Consume(ConsumeContext<FixtureNoteCreated> context) => Task.CompletedTask;
}
