using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SlotBook.ServiceDefaults;

public static class ServiceDefaultsExtensions
{
    /// <summary>Tag for checks that run on the liveness endpoints.</summary>
    public const string LiveTag = "live";

    /// <summary>
    /// Writes JSON console logs whose scope carries the W3C trace id of the current request
    /// (AD-17), and registers health checks.
    /// </summary>
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Logging.Configure(options =>
            options.ActivityTrackingOptions = ActivityTrackingOptions.TraceId | ActivityTrackingOptions.SpanId);
        builder.Logging.AddJsonConsole(options =>
        {
            options.IncludeScopes = true;
            options.UseUtcTimestamp = true;
        });
        builder.Services.AddHealthChecks();

        return builder;
    }

    /// <summary>Adds the one-line-per-request log: method, path without query string, status.</summary>
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<RequestLoggingMiddleware>();
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Liveness never runs dependency checks; readiness runs every registered check.
        foreach (var path in new[] { "/health", "/api/health" })
        {
            app.MapHealthChecks(path, new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains(LiveTag),
                ResponseWriter = WriteStatusAsync,
            }).AllowAnonymous();
        }

        // The build's commit SHA lets a deploy pipeline confirm the new version is the one answering.
        // Render sets RENDER_GIT_COMMIT; another host would map its own variable to the same key.
        var commit = ReadCommit(app.Configuration["RENDER_GIT_COMMIT"]);
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            ResponseWriter = (context, report) => WriteReadinessAsync(context, report, commit),
        }).AllowAnonymous();

        return app;
    }

    // Names and statuses only: descriptions and exception text could leak secrets.
    private static Task WriteReadinessAsync(HttpContext context, HealthReport report, string? commit)
    {
        var checks = report.Entries.ToDictionary(entry => entry.Key, entry => entry.Value.Status.ToString());
        var status = report.Status.ToString();

        // Left out (not null) when unknown, so local and test bodies stay unchanged.
        return commit is null
            ? context.Response.WriteAsJsonAsync(new { status, checks })
            : context.Response.WriteAsJsonAsync(new { status, commit, checks });
    }

    // Only a 7-40 character lowercase hex SHA is echoed to the anonymous caller.
    private static string? ReadCommit(string? value) =>
        value is { Length: >= 7 and <= 40 } && value.All(c => c is (>= '0' and <= '9') or (>= 'a' and <= 'f'))
            ? value
            : null;

    private static Task WriteStatusAsync(HttpContext context, HealthReport report) =>
        context.Response.WriteAsJsonAsync(new { status = report.Status.ToString() });
}
