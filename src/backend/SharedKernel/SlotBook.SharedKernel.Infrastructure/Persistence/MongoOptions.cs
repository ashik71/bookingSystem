namespace SlotBook.SharedKernel.Infrastructure.Persistence;

/// <summary>Bound from <c>ConnectionStrings:mongodb</c>, <c>Mongo:Database</c> and the optional schema timings.</summary>
internal sealed class MongoOptions
{
    public const string ConnectionStringKey = "ConnectionStrings:mongodb";

    public const string DatabaseKey = "Mongo:Database";

    public const string SchemaTimeoutKey = "Mongo:SchemaTimeoutSeconds";

    public const string MigrationLockExpiryKey = "Mongo:MigrationLockExpirySeconds";

    public string? ConnectionString { get; set; }

    public string? Database { get; set; }

    /// <summary>How long startup may spend on the schema (collections, migrations, indexes).</summary>
    public int SchemaTimeoutSeconds { get; set; } = 60;

    /// <summary>How long a migration lock lives when its holder crashes.</summary>
    public int MigrationLockExpirySeconds { get; set; } = 300;
}
