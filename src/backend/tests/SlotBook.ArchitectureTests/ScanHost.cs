#pragma warning disable CA1515 // public: discovered by MVC through an application part
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SlotBook.SharedKernel.Infrastructure.Persistence;

namespace SlotBook.ArchitectureTests;

/// <summary>
/// The real API host with dummy configuration, only to read its endpoint table. The Mongo client
/// never connects, and nothing is requested.
/// </summary>
internal sealed class ScanHost(bool includeFixtureControllers) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Production");
        builder.UseSetting("ConnectionStrings:mongodb", "mongodb://localhost:27017");
        builder.UseSetting("Tenancy:Tenant:Slug", "scan-clinic");
        builder.UseSetting("Tenancy:Tenant:DisplayName", "Scan Clinic");
        builder.UseSetting("Tenancy:Tenant:Country", "GB");
        builder.UseSetting("Tenancy:Tenant:TimeZone", "Europe/London");
        builder.UseSetting("Tenancy:Tenant:WeekStartDay", "Monday");

        // The host is never started against a database, so the startup schema work stays out.
        builder.ConfigureServices(services =>
        {
            var schema = services.Where(descriptor => descriptor.ImplementationType == typeof(SchemaInitializer)).ToList();
            foreach (var descriptor in schema)
            {
                services.Remove(descriptor);
            }
        });

        if (includeFixtureControllers)
        {
            builder.ConfigureServices(services =>
                services.AddControllers().AddApplicationPart(typeof(ScanHost).Assembly));
        }
    }
}
