using System.Reflection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using SlotBook.SharedKernel.Application;
using SlotBook.SharedKernel.Application.Persistence;
using SlotBook.SharedKernel.Infrastructure.Persistence;
using SlotBook.SharedKernel.Infrastructure.Persistence.DataProtection;
using SlotBook.SharedKernel.Infrastructure.Persistence.Leases;
using SlotBook.SharedKernel.Infrastructure.Persistence.Migrations;
using SlotBook.SharedKernel.Infrastructure.Tenancy;
using SlotBook.SharedKernel.Infrastructure.Time;

namespace SlotBook.SharedKernel.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IClock"/>, the configured tenant, the MongoDB client and database, the unit of work and the tenant-filtered collections, with the readiness
    /// check. Startup fails when <c>Tenancy:Tenant</c>, <c>ConnectionStrings:mongodb</c> or <c>Mongo:Database</c> is missing
    /// or invalid. Needs a <see cref="TimeProvider"/> registered.
    /// </summary>
    public static IServiceCollection AddSharedKernel(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        MongoConventions.Apply();

        services.AddSingleton<IClock, Clock>();

        services.AddOptions<TenantOptions>()
            .Bind(configuration.GetSection(TenantOptions.SectionName))
            .ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<TenantOptions>, TenantOptionsValidator>());
        services.AddSingleton<ITenantContext, ConfiguredTenantContext>();

        services.AddOptions<MongoOptions>()
            .Configure(options =>
            {
                options.ConnectionString = configuration[MongoOptions.ConnectionStringKey];
                options.Database = configuration[MongoOptions.DatabaseKey];
                options.SchemaTimeoutSeconds = configuration.GetValue(MongoOptions.SchemaTimeoutKey, options.SchemaTimeoutSeconds);
                options.MigrationLockExpirySeconds = configuration.GetValue(MongoOptions.MigrationLockExpiryKey, options.MigrationLockExpirySeconds);
            })
            .ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<MongoOptions>, MongoOptionsValidator>());

        // The client doesn't connect until first use, so startup doesn't wait on the server.
        services.AddSingleton<IMongoClient>(provider =>
        {
            var settings = MongoClientSettings.FromUrl(MongoUrl.Create(provider.GetRequiredService<IOptions<MongoOptions>>().Value.ConnectionString));

            // AD-17: spans name the operation, never the command text (0 keeps db.query.text off).
            settings.TracingOptions = new TracingOptions { QueryTextMaxLength = 0 };
            return new MongoClient(settings);
        });
        services.AddSingleton(provider =>
            provider.GetRequiredService<IMongoClient>().GetDatabase(provider.GetRequiredService<IOptions<MongoOptions>>().Value.Database));
        services.AddSingleton<MongoXmlRepository>();

        // Keys at rest rely on Atlas encryption (NFR-10), so the "no XML encryptor" startup warning is expected.
        services.AddDataProtection().SetApplicationName("SlotBook");
        services.AddOptions<KeyManagementOptions>()
            .Configure<MongoXmlRepository>((options, repository) => options.XmlRepository = repository);
        services.AddSingleton<TenantCollections>();
        services.AddSingleton<RetryableDuplicateKeys>();
        services.AddSingleton<ICommandExecution, MongoCommandExecution>();
        services.AddScoped<MongoUnitOfWork>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<MongoUnitOfWork>());
        services.AddSingleton<LeaseHolder>();
        services.AddSingleton<LeaseStore>();
        services.AddSingleton<HeartbeatStateStore>();
        services.AddSingleton<MigrationLock>();
        services.AddSingleton<MigrationRunner>();
        services.AddHostedService<SchemaInitializer>();
        services.AddHealthChecks()
            .AddCheck<MongoReadinessCheck>("mongodb", failureStatus: null, tags: null, timeout: TimeSpan.FromSeconds(5));

        return services;
    }

    /// <summary>Registers every concrete <see cref="IIndexConfiguration"/> of the assembly. Startup creates their indexes.</summary>
    public static IServiceCollection AddIndexConfigurationsFrom(this IServiceCollection services, Assembly assembly) =>
        AddAll<IIndexConfiguration>(services, assembly);

    /// <summary>Registers every concrete <see cref="IMigration"/> of the assembly. Startup runs the ones not yet recorded.</summary>
    public static IServiceCollection AddMigrationsFrom(this IServiceCollection services, Assembly assembly) =>
        AddAll<IMigration>(services, assembly);

    private static IServiceCollection AddAll<TService>(IServiceCollection services, Assembly assembly)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        foreach (var type in assembly.GetTypes().Where(type => type is { IsAbstract: false, IsInterface: false } && typeof(TService).IsAssignableFrom(type)))
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(TService), type));
        }

        return services;
    }
}
