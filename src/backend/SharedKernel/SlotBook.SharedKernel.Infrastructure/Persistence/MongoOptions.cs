namespace SlotBook.SharedKernel.Infrastructure.Persistence;

/// <summary>Bound from <c>ConnectionStrings:mongodb</c> and <c>Mongo:Database</c>.</summary>
internal sealed class MongoOptions
{
    public const string ConnectionStringKey = "ConnectionStrings:mongodb";

    public const string DatabaseKey = "Mongo:Database";

    public string? ConnectionString { get; set; }

    public string? Database { get; set; }
}
