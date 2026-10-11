using System.Buffers;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace SlotBook.SharedKernel.Infrastructure.Persistence;

internal sealed class MongoOptionsValidator : IValidateOptions<MongoOptions>
{
    private const int MaxDatabaseNameLength = 63;

    private static readonly SearchValues<char> _forbiddenInDatabaseName = SearchValues.Create('/', '\\', '.', ' ', '"', '$', '\0');

    public ValidateOptionsResult Validate(string? name, MongoOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return ValidateOptionsResult.Fail($"{MongoOptions.ConnectionStringKey} is required");
        }

        try
        {
            _ = MongoUrl.Create(options.ConnectionString);
        }
        catch (MongoConfigurationException)
        {
            // The driver's message can echo the connection string, so it is not repeated here.
            return ValidateOptionsResult.Fail($"{MongoOptions.ConnectionStringKey} is not a valid MongoDB connection string");
        }

        if (string.IsNullOrWhiteSpace(options.Database))
        {
            return ValidateOptionsResult.Fail($"{MongoOptions.DatabaseKey} is required");
        }

        if (options.Database.Length > MaxDatabaseNameLength || options.Database.AsSpan().IndexOfAny(_forbiddenInDatabaseName) >= 0)
        {
            return ValidateOptionsResult.Fail($"{MongoOptions.DatabaseKey} is not a valid MongoDB database name");
        }

        return ValidateOptionsResult.Success;
    }
}
