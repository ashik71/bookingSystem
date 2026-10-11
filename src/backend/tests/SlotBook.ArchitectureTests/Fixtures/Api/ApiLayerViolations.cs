#pragma warning disable CA1812, CA1852 // fixture: a deliberate violation that is never instantiated
namespace SlotBook.Api.Fixture;

/// <summary>Deliberate AD-2 api layer violation: depends on MongoDB.</summary>
internal sealed class ApiLayerViolations
{
    public MongoDB.Bson.ObjectId Id { get; private set; }
}
