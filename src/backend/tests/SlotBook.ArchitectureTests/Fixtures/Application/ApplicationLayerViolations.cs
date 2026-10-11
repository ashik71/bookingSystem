#pragma warning disable CA1812, CA1852 // fixture: a deliberate violation that is never instantiated
using SlotBook.Modules.Fixture.Infrastructure;

namespace SlotBook.Modules.Fixture.Application;

/// <summary>Deliberate AD-2 application layer violations: depends on Infrastructure and MongoDB.</summary>
internal sealed class ApplicationLayerViolations
{
    public FixtureInfrastructureType? Infrastructure { get; private set; }

    public MongoDB.Bson.ObjectId Id { get; private set; }
}
