#pragma warning disable CA1812, CA1852 // fixture: a deliberate violation that is never instantiated
using MongoDB.Bson;
using MongoDB.Driver;

namespace SlotBook.Modules.Fixture.Infrastructure;

/// <summary>Deliberate AD-5 violation: asks the database for a collection without the tenant filter.</summary>
internal sealed class UnfilteredCollectionViolation(IMongoDatabase database)
{
    public IMongoCollection<BsonDocument> Bookings() => database.GetCollection<BsonDocument>("bookings");
}
