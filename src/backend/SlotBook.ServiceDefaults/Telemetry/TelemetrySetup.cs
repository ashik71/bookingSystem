using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace SlotBook.ServiceDefaults.Telemetry;

internal static class TelemetrySetup
{
    private static readonly string[] _untracedPaths = ["/health", "/api/health", "/health/ready"];

    /// <summary>
    /// Traces, metrics and logs through OpenTelemetry. They are exported over OTLP only when
    /// <c>OTEL_EXPORTER_OTLP_ENDPOINT</c> is set; the protocol and headers come from the standard
    /// <c>OTEL_EXPORTER_OTLP_*</c> variables, so no vendor detail lives in the repo.
    /// </summary>
    internal static void AddSlotBookTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        var environmentName = builder.Environment.EnvironmentName;
        var commit = BuildInfo.ReadCommit(builder.Configuration["RENDER_GIT_COMMIT"]);

        builder.Logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
        });

        var otel = builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource =>
            {
                resource.AddService("slotbook-api", serviceVersion: commit);
                resource.AddAttributes([new("deployment.environment.name", environmentName)]);
            })
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(options =>
                {
                    options.RecordException = true;
                    options.Filter = IsTraced;
                })
                .AddSource("MassTransit", "MongoDB.Driver", "SlotBook.*")
                .AddProcessor<SensitiveTagProcessor>())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter("MassTransit", "SlotBook.*"));

        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            otel.UseOtlpExporter();
        }
    }

    private static bool IsTraced(HttpContext context) =>
        !_untracedPaths.Contains(context.Request.Path.Value, StringComparer.OrdinalIgnoreCase);
}
