#pragma warning disable CA1812, CA1852 // fixture: a deliberate violation that is never instantiated
using SlotBook.Modules.Fixture.Application;
using SlotBook.Modules.Fixture.Infrastructure;

namespace SlotBook.Modules.Fixture.Domain;

/// <summary>Deliberate AD-2 domain layer violations: depends on Application, Infrastructure and MongoDB.</summary>
internal sealed class DomainLayerViolations
{
    public FixtureApplicationType? Application { get; private set; }

    public FixtureInfrastructureType? Infrastructure { get; private set; }

    public MongoDB.Bson.ObjectId Id { get; private set; }
}
