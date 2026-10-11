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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Production");

        builder.ConfigureServices(services =>
            services.AddControllers().AddApplicationPart(typeof(FailingTestController).Assembly));
        builder.ConfigureLogging(logging => logging.AddProvider(Logs));
    }
}
