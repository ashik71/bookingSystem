#pragma warning disable CA1515 // public: used as an xunit fixture type, or discovered by MVC
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SlotBook.IntegrationTests.TestSupport;

namespace SlotBook.IntegrationTests;

public sealed class SlotBookApiFactory : WebApplicationFactory<Program>
{
    public CapturingLoggerProvider Logs { get; } = new();

    /// <summary>Slug of the configured tenant; <see langword="null"/> leaves <c>Tenancy:Tenant</c> out.</summary>
    public string? TenantSlug { get; init; } = "test-clinic";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Production");

        if (TenantSlug is not null)
        {
            builder.UseSetting("Tenancy:Tenant:Slug", TenantSlug);
            builder.UseSetting("Tenancy:Tenant:DisplayName", "Test Clinic");
            builder.UseSetting("Tenancy:Tenant:Country", "GB");
            builder.UseSetting("Tenancy:Tenant:TimeZone", "Europe/London");
            builder.UseSetting("Tenancy:Tenant:WeekStartDay", "Monday");
        }

        builder.ConfigureServices(services =>
            services.AddControllers().AddApplicationPart(typeof(FailingTestController).Assembly));
        builder.ConfigureLogging(logging => logging.AddProvider(Logs));
    }
}
