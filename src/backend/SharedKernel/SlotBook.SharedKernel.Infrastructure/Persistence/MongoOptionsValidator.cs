using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace SlotBook.SharedKernel.Infrastructure.Persistence;

internal sealed class MongoOptionsValidator : IValidateOptions<MongoOptions>
{
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

        return ValidateOptionsResult.Success;
    }
}
