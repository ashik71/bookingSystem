using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace SlotBook.SharedKernel.Infrastructure.Persistence;

/// <summary>
/// Pings MongoDB. The result carries no exception text: driver messages can contain the
/// connection string.
/// </summary>
internal sealed class MongoReadinessCheck(IMongoClient client, ILogger<MongoReadinessCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await client.GetDatabase("admin")
                .RunCommandAsync<BsonDocument>(new BsonDocument("ping", 1), cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogMongoPingFailed(exception.GetType().Name);
            return HealthCheckResult.Unhealthy();
        }
    }
}
