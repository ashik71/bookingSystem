using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SlotBook.SharedKernel.Application;
using SlotBook.SharedKernel.Infrastructure.Persistence;
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
            })
            .ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<MongoOptions>, MongoOptionsValidator>());

        // The client doesn't connect until first use, so startup doesn't wait on the server.
        services.AddSingleton<IMongoClient>(provider =>
            new MongoClient(MongoClientSettings.FromUrl(MongoUrl.Create(provider.GetRequiredService<IOptions<MongoOptions>>().Value.ConnectionString))));
        services.AddSingleton(provider =>
            provider.GetRequiredService<IMongoClient>().GetDatabase(provider.GetRequiredService<IOptions<MongoOptions>>().Value.Database));
        services.AddSingleton<TenantCollections>();
        services.AddScoped<MongoUnitOfWork>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<MongoUnitOfWork>());
        services.AddHealthChecks()
            .AddCheck<MongoReadinessCheck>("mongodb", failureStatus: null, tags: null, timeout: TimeSpan.FromSeconds(5));

        return services;
    }
}
