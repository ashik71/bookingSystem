using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;

namespace SlotBook.SharedKernel.Infrastructure.Persistence.DataProtection;

/// <summary>
/// Data Protection keys in the global <c>data_protection_keys</c> collection (<c>_id</c> = the key's friendly name, <c>xml</c>), so a token protected before a restart
/// or by another host can be unprotected. The interface is synchronous, so this uses the synchronous driver API, outside any transaction.
/// Two hosts creating a key at once store two keys, which Data Protection handles.
/// </summary>
internal sealed class MongoXmlRepository(IMongoDatabase database) : IXmlRepository
{
    internal const string CollectionName = "data_protection_keys";

    private IMongoCollection<BsonDocument> Keys => database.GetCollection<BsonDocument>(CollectionName);

    public IReadOnlyCollection<XElement> GetAllElements() =>
        [.. Keys.Find(FilterDefinition<BsonDocument>.Empty).ToList().Select(document => XElement.Parse(document["xml"].AsString))];

    public void StoreElement(XElement element, string friendlyName)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(friendlyName);

        Keys.InsertOne(new BsonDocument { { "_id", friendlyName }, { "xml", element.ToString(SaveOptions.DisableFormatting) } });
    }
}
