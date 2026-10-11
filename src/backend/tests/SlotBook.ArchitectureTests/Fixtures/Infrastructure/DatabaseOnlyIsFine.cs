#pragma warning disable CA1812, CA1852 // fixture: never instantiated
using MongoDB.Driver;

namespace SlotBook.Modules.Fixture.Infrastructure;

/// <summary>Not a violation: gets the database, not a collection.</summary>
internal sealed class DatabaseOnlyIsFine(IMongoClient client)
{
    public IMongoDatabase Database() => client.GetDatabase("slotbook");
}
