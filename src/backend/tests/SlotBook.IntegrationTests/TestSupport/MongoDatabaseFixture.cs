#pragma warning disable CA1515 // public: used as an xunit fixture type
using System.Security.Cryptography;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace SlotBook.IntegrationTests.TestSupport;

/// <summary>One MongoDB database per test class, dropped when the class finishes.</summary>
public sealed class MongoDatabaseFixture : IAsyncLifetime
{
    private const int MaxNameLength = 63;

    private readonly MongoClient _client = new(IntegrationEnvironment.MongoDb);

    public MongoDatabaseFixture()
    {
        DatabaseName = NewDatabaseName(TestContext.Current.TestClass?.TestClassSimpleName);
        Database = _client.GetDatabase(DatabaseName);
    }

    public IMongoClient Client => _client;

    public IMongoDatabase Database { get; }

    public string DatabaseName { get; }

    public async ValueTask InitializeAsync()
    {
        var hello = await Database.RunCommandAsync<BsonDocument>(
            new BsonDocument("hello", 1), cancellationToken: TestContext.Current.CancellationToken);
        if (!hello.Contains("setName") || !hello.GetValue("isWritablePrimary", false).AsBoolean)
        {
            throw new InvalidOperationException(
                "ConnectionStrings__mongodb must point at a writable replica-set primary "
                + "(mongod --replSet rs0, initiated): multi-document transactions need one (architecture AD-19).");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DropDatabaseAsync(DatabaseName);
        _client.Dispose();
    }

    internal static string NewDatabaseName(string? className)
    {
        var suffix = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(6));
        var safe = new string((className ?? string.Empty).Where(char.IsAsciiLetterOrDigit).ToArray());
        var prefix = $"it_{safe}";
        var room = MaxNameLength - suffix.Length - 1;
        if (prefix.Length > room)
        {
            prefix = prefix[..room];
        }

        return $"{prefix}_{suffix}";
    }
}
