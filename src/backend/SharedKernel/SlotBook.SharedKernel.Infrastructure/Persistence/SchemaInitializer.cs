using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SlotBook.SharedKernel.Infrastructure.Persistence.Migrations;

namespace SlotBook.SharedKernel.Infrastructure.Persistence;

/// <summary>
/// Blocks startup (before Kestrel listens) until the schema is ready: creates the collections, runs the
/// migrations, then creates the indexes (expand / contract). Fails startup when MongoDB is unreachable or
/// the work takes longer than <c>Mongo:SchemaTimeoutSeconds</c> (default 60).
/// </summary>
internal sealed class SchemaInitializer : IHostedService
{
    private const int NamespaceExists = 48;

    private readonly IMongoDatabase _database;
    private readonly IReadOnlyList<IIndexConfiguration> _configurations;
    private readonly MigrationRunner _migrations;
    private readonly RetryableDuplicateKeys _retryable;
    private readonly IOptions<MongoOptions> _options;
    private readonly ILogger<SchemaInitializer> _logger;

    public SchemaInitializer(
        IMongoDatabase database,
        IEnumerable<IIndexConfiguration> configurations,
        MigrationRunner migrations,
        RetryableDuplicateKeys retryable,
        IOptions<MongoOptions> options,
        ILogger<SchemaInitializer> logger)
    {
        _database = database;
        _configurations = [.. configurations];
        _migrations = migrations;
        _retryable = retryable;
        _options = options;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(_options.Value.SchemaTimeoutSeconds);
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        budget.CancelAfter(timeout);
        try
        {
            await InitializeAsync(budget.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException($"The MongoDB schema was not ready within {timeout.TotalSeconds:0} s ({MongoOptions.SchemaTimeoutKey}).", exception);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task InitializeAsync(CancellationToken cancellationToken)
    {
        Validate();

        var names = _configurations.Select(configuration => configuration.CollectionName)
            .Append(MigrationLock.CollectionName)
            .Append(MigrationRunner.CollectionName)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        await CreateCollectionsAsync(names, cancellationToken).ConfigureAwait(false);

        var applied = await _migrations.RunAsync(cancellationToken).ConfigureAwait(false);

        var indexCount = 0;
        foreach (var configuration in _configurations)
        {
            if (configuration.Indexes.Count > 0)
            {
                await _database.GetCollection<MongoDB.Bson.BsonDocument>(configuration.CollectionName)
                    .Indexes.CreateManyAsync(configuration.Indexes, cancellationToken).ConfigureAwait(false);
                indexCount += configuration.Indexes.Count;
            }

            foreach (var index in configuration.RetryableIndexes)
            {
                _retryable.Add(configuration.CollectionName, index);
            }
        }

        _logger.LogSchemaReady(names.Count, indexCount, applied);
    }

    private void Validate()
    {
        foreach (var configuration in _configurations)
        {
            var names = configuration.Indexes.Select(index => index.Options.Name).ToList();
            foreach (var name in names.Where(name => name is null || !(name.StartsWith("ux_", StringComparison.Ordinal) || name.StartsWith("ix_", StringComparison.Ordinal))))
            {
                throw new InvalidOperationException($"Index '{name}' of '{configuration.CollectionName}' must have an explicit name starting with 'ux_' or 'ix_'.");
            }

            foreach (var missing in configuration.RetryableIndexes.Except(names, StringComparer.Ordinal))
            {
                throw new InvalidOperationException($"Retryable index '{missing}' is not an index of '{configuration.CollectionName}'.");
            }
        }
    }

    private async Task CreateCollectionsAsync(List<string> names, CancellationToken cancellationToken)
    {
        // Implicit creation inside a snapshot transaction is restricted, so every collection exists before first use.
        var existing = await (await _database.ListCollectionNamesAsync(cancellationToken: cancellationToken).ConfigureAwait(false))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var name in names.Except(existing, StringComparer.Ordinal))
        {
            try
            {
                await _database.CreateCollectionAsync(name, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (MongoCommandException exception) when (exception.Code == NamespaceExists)
            {
                // Another instance created it first.
            }
        }
    }
}
