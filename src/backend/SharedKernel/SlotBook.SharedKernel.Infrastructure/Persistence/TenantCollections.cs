using MongoDB.Driver;
using SlotBook.SharedKernel.Application;
using SlotBook.SharedKernel.Domain;

namespace SlotBook.SharedKernel.Infrastructure.Persistence;

/// <summary>Hands out the tenant-filtered collections for the write repositories (AD-5).</summary>
public sealed class TenantCollections
{
    private readonly IMongoDatabase _database;
    private readonly ITenantContext _tenant;

    public TenantCollections(IMongoDatabase database, ITenantContext tenant)
    {
        _database = database;
        _tenant = tenant;
        MongoConventions.Apply();
    }

    internal TenantCollection<T> For<T>(string name)
        where T : AggregateRoot =>
        new(_database, name, _tenant.TenantId);
}
