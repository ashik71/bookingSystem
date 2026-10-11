#pragma warning disable CA1812, CA1852 // fixture: a deliberate violation that is never instantiated
namespace SlotBook.Modules.Fixture.Domain;

/// <summary>Deliberate AD-4 violations: setters that are visible outside the type.</summary>
internal class SetterViolations
{
    public string? PublicSet { get; set; }

    public string? PublicInit { get; init; }

    public string? InternalSet { get; internal set; }

    public string? ProtectedSet { get; protected set; }

    public string? PrivateSetIsFine { get; private set; }

    public string? NoSetterIsFine { get; }
}
