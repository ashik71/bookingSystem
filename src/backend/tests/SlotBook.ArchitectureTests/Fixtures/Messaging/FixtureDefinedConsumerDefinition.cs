#pragma warning disable CA1515, CA1812 // fixture: never used
using SlotBook.SharedKernel.Infrastructure.Messaging;

namespace SlotBook.Modules.Fixture.Messaging;

public sealed class FixtureDefinedConsumerDefinition(EndpointNaming naming)
    : SlotBookConsumerDefinition<FixtureDefinedConsumer>(naming, "fixture", "defined");
