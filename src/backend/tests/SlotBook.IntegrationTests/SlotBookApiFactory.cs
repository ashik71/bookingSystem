#pragma warning disable CA1515 // public: used as an xunit fixture type, or discovered by MVC
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SlotBook.IntegrationTests.TestSupport;
using SlotBook.IntegrationTests.TestSupport.Guards;
using SlotBook.IntegrationTests.TestSupport.Persistence;
using SlotBook.SharedKernel.Application.Dispatching;

namespace SlotBook.IntegrationTests;

public sealed class SlotBookApiFactory : WebApplicationFactory<Program>
{
    private string? _mongoConnectionString;
    private bool _mongoConnectionStringSet;

    public CapturingLoggerProvider Logs { get; } = new();

    /// <summary>Extra services, for example a test index configuration or migration.</summary>
    public Action<IServiceCollection>? ConfigureTestServices { get; init; }

    /// <summary>Extra configuration, for example <c>Mongo:MigrationLockExpirySeconds</c>.</summary>
    public IReadOnlyDictionary<string, string?> Settings { get; init; } = new Dictionary<string, string?>();

    /// <summary><c>Mongo:Database</c>; <see langword="null"/> keeps the default of appsettings.json.</summary>
    public string? MongoDatabase { get; init; }

    /// <summary>Slug of the configured tenant; <see langword="null"/> leaves <c>Tenancy:Tenant</c> out.</summary>
    public string? TenantSlug { get; init; } = "test-clinic";

    /// <summary>
    /// <c>ConnectionStrings:mongodb</c>; defaults to the real MongoDB of the environment,
    /// <see langword="null"/> leaves it out.
    /// </summary>
    public string? MongoConnectionString
    {
        get => _mongoConnectionStringSet ? _mongoConnectionString : IntegrationEnvironment.MongoDb;
        init
        {
            _mongoConnectionString = value;
            _mongoConnectionStringSet = true;
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Production");

        if (MongoConnectionString is not null)
        {
            builder.UseSetting("ConnectionStrings:mongodb", MongoConnectionString);
        }
        else
        {
            // The process environment may carry the variable (CI, sandbox); blank it explicitly.
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection([new("ConnectionStrings:mongodb", string.Empty)]));
        }

        if (MongoDatabase is not null)
        {
            builder.UseSetting("Mongo:Database", MongoDatabase);
        }

        foreach (var (key, value) in Settings)
        {
            builder.UseSetting(key, value);
        }

        if (TenantSlug is not null)
        {
            builder.UseSetting("Tenancy:Tenant:Slug", TenantSlug);
            builder.UseSetting("Tenancy:Tenant:DisplayName", "Test Clinic");
            builder.UseSetting("Tenancy:Tenant:Country", "GB");
            builder.UseSetting("Tenancy:Tenant:TimeZone", "Europe/London");
            builder.UseSetting("Tenancy:Tenant:WeekStartDay", "Monday");
        }

        builder.ConfigureServices(services =>
        {
            services.AddSingleton<HandlerProbe>();
            services.AddScoped<NoteWriteRepository>();
            services.AddScoped<GlobalNoteWriteRepository>();
            services.AddScoped<GuardCounterWriteRepository>();
            services.AddSingleton<ConflictAttempts>();
            ConfigureTestServices?.Invoke(services);
            services.AddUseCasesFrom(typeof(DispatchTestController).Assembly);
            services.AddControllers().AddApplicationPart(typeof(FailingTestController).Assembly);
        });
        builder.ConfigureLogging(logging => logging.AddProvider(Logs));
    }
}
