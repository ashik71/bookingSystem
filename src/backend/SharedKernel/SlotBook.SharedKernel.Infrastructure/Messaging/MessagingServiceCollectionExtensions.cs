using System.Reflection;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace SlotBook.SharedKernel.Infrastructure.Messaging;

public static class MessagingServiceCollectionExtensions
{
    /// <summary>Optional prefix for endpoint (queue) names. Tests use it to keep their queues apart.</summary>
    public const string EndpointPrefixKey = "Messaging:EndpointPrefix";

    /// <summary>
    /// Registers MassTransit over RabbitMQ with the MongoDB bus outbox (AD-10, AD-7). Events staged with
    /// <c>IUnitOfWork.AddIntegrationEvent</c> are written to the outbox on the unit of work's own session
    /// and delivered to the broker after the commit. Consumers (and their definitions) are scanned from <paramref name="consumerAssemblies"/>;
    /// MassTransit only sees public types, so a consumer must be public.
    /// Startup fails when <c>ConnectionStrings:rabbitmq</c> is missing or not an amqp URI; it does not wait for
    /// the broker, and the bus is kept out of <c>/health/ready</c> (the outbox exists so that commits
    /// succeed while the broker is down). Call it after <c>AddSharedKernel</c>, which registers the MongoDB
    /// client and database the outbox reuses.
    /// </summary>
    public static IServiceCollection AddSlotBookMessaging(this IServiceCollection services, IConfiguration configuration, params Assembly[] consumerAssemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(consumerAssemblies);

        services.AddOptions<RabbitMqOptions>()
            .Configure(options => options.ConnectionString = configuration[RabbitMqOptions.ConnectionStringKey])
            .ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<RabbitMqOptions>, RabbitMqOptionsValidator>());

        var prefix = configuration[EndpointPrefixKey];
        services.AddMassTransit(bus =>
        {
            if (string.IsNullOrEmpty(prefix))
            {
                bus.SetKebabCaseEndpointNameFormatter();
            }
            else
            {
                bus.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(prefix, includeNamespace: false));
            }

            if (consumerAssemblies.Length > 0)
            {
                bus.AddConsumers(consumerAssemblies);
            }

            bus.AddMongoDbOutbox(outbox =>
            {
                // The one client and database AddSharedKernel registered: one connection pool.
                outbox.ClientFactory(provider => provider.GetRequiredService<IMongoClient>());
                outbox.DatabaseFactory(provider => provider.GetRequiredService<IMongoDatabase>());
                outbox.UseBusOutbox();

                // Nothing consumes through the inbox yet, and the cleanup service creates its index on start,
                // which would stop the host when MongoDB is down. The consumer story turns it back on.
                outbox.DisableInboxCleanupService();
            });

            bus.UsingRabbitMq((context, rabbit) =>
            {
                rabbit.Host(new Uri(context.GetRequiredService<IOptions<RabbitMqOptions>>().Value.ConnectionString!));
                rabbit.ConfigureEndpoints(context);
            });
        });

        // MassTransit's own context starts transactions of its own. Ours runs on the unit of work's session.
        services.RemoveAll<MassTransit.MongoDbIntegration.MongoDbContext>();
        services.AddScoped<MassTransit.MongoDbIntegration.MongoDbContext, UnitOfWorkMongoDbContext>();

        services.Configure<HealthCheckServiceOptions>(options =>
        {
            foreach (var registration in options.Registrations.Where(registration => registration.Name.StartsWith("masstransit", StringComparison.OrdinalIgnoreCase)).ToList())
            {
                options.Registrations.Remove(registration);
            }
        });

        return services;
    }
}
