namespace SlotBook.SharedKernel.Infrastructure.Persistence;

/// <summary>Bound from <c>ConnectionStrings:mongodb</c>.</summary>
internal sealed class MongoOptions
{
    public const string ConnectionStringKey = "ConnectionStrings:mongodb";

    public string? ConnectionString { get; set; }
}
