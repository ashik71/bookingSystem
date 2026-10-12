using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SlotBook.SharedKernel.Application.Dispatching;
using SlotBook.SharedKernel.Infrastructure;
using SlotBook.SharedKernel.Infrastructure.Messaging;

namespace SlotBook.IntegrationTests.TestSupport.Messaging;

/// <summary>
/// A host without the web part for the consume tests: the shared kernel, the dispatcher, the use cases of
/// the test assembly and the bus with this assembly's consumers. The consumers cannot be added to the API
/// factory, because the API passes the consumer assemblies itself.
/// </summary>
internal sealed class ConsumerHost : IAsyncDisposable
{
    private ConsumerHost(IHost host, string endpointPrefix, CapturingLoggerProvider logs)
    {
        Host = host;
        EndpointPrefix = endpointPrefix;
        Logs = logs;
    }

    public IHost Host { get; }

    public string EndpointPrefix { get; }

    public CapturingLoggerProvider Logs { get; }

    public ConsumerProbe Probe => Host.Services.GetRequiredService<ConsumerProbe>();

    /// <summary>The receive endpoint (queue) of <see cref="NoteCreatedConsumer"/>.</summary>
    public string Endpoint => $"{EndpointPrefix}-notes-record-note-seen";

    public static async Task<ConsumerHost> StartAsync(string mongoDatabase, string tenantSlug, CancellationToken cancellationToken)
    {
        var prefix = "it-" + Convert.ToHexStringLower(System.Security.Cryptography.RandomNumberGenerator.GetBytes(4));
        var logs = new CapturingLoggerProvider();
        var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["ConnectionStrings:mongodb"] = IntegrationEnvironment.MongoDb,
            ["ConnectionStrings:rabbitmq"] = IntegrationEnvironment.RabbitMq,
            ["Mongo:Database"] = mongoDatabase,
            ["Messaging:EndpointPrefix"] = prefix,
            ["Tenancy:Tenant:Slug"] = tenantSlug,
            ["Tenancy:Tenant:DisplayName"] = "Test Clinic",
            ["Tenancy:Tenant:Country"] = "GB",
            ["Tenancy:Tenant:TimeZone"] = "Europe/London",
            ["Tenancy:Tenant:WeekStartDay"] = "Monday",
        });
#pragma warning disable RS0030 // a test host is a composition root
        builder.Services.AddSingleton(TimeProvider.System);
#pragma warning restore RS0030
        builder.Services.AddSharedKernel(builder.Configuration);
        builder.Services.AddSlotBookMessaging(builder.Configuration, typeof(ConsumerHost).Assembly);
        builder.Services.AddDispatcher();
        builder.Services.AddUseCasesFrom(typeof(ConsumerHost).Assembly);
        builder.Services.AddSingleton<ConsumerProbe>();
        builder.Services.AddScoped<NoteSeenWriteRepository>();
        builder.Services.AddScoped<Persistence.NoteWriteRepository>();
        builder.Services.AddSingleton<Guards.ConflictAttempts>();
        builder.Logging.AddProvider(logs);
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
        builder.Logging.AddFilter(null, LogLevel.Debug);

        var host = builder.Build();
        await host.StartAsync(cancellationToken).ConfigureAwait(false);

        // The bus starts in the background; wait until it is connected and the endpoints are ready.
        await host.Services.GetRequiredService<IBusControl>().WaitForHealthStatus(BusHealthStatus.Healthy, TimeSpan.FromSeconds(20)).ConfigureAwait(false);
        return new ConsumerHost(host, prefix, logs);
    }

    public async ValueTask DisposeAsync()
    {
        await Host.StopAsync().ConfigureAwait(false);
        Host.Dispose();
    }
}
