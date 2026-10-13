using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;
using OpenTelemetry.Trace;
using Shouldly;
using Xunit;

namespace SlotBook.IntegrationTests.Observability;

public class TracingTests(SlotBookApiFactory factory) : IClassFixture<SlotBookApiFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task RequestProducesServerSpanWithRouteAndStatus()
    {
        var client = Https(factory);
        const string traceId = "5bf92f3577b34da6a3ce929d0e0e4736";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/xsrf-token");
        request.Headers.Add("traceparent", $"00-{traceId}-00f067aa0ba902b7-01");

        var response = await client.SendAsync(request, Ct);

        response.IsSuccessStatusCode.ShouldBeTrue();
        var span = await factory.Telemetry.WaitForSpanAsync(s => s.Kind == ActivityKind.Server && s.TraceId.ToHexString() == traceId, Ct);
        span.GetTagItem("http.response.status_code").ShouldBe(204);
        span.GetTagItem("http.route").ShouldNotBeNull();
    }

    [Fact]
    public async Task ErrorTraceIdMatchesSpanAndRequestLog()
    {
        var client = Https(factory);

        var response = await client.GetAsync("/test-only/throw", Ct);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        var traceId = body.RootElement.GetProperty("traceId").GetString();
        traceId.ShouldNotBeNull();
        await factory.Telemetry.WaitForSpanAsync(s => s.Kind == ActivityKind.Server && s.TraceId.ToHexString() == traceId, Ct);
        factory.Logs.Records.ShouldContain(r => r.EventId == 2 && r.TraceId == traceId);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/api/health")]
    [InlineData("/health/ready")]
    public async Task HealthEndpointsProduceNoSpan(string path)
    {
        using var isolated = new SlotBookApiFactory();
        var client = Https(isolated);

        await client.GetAsync(path, Ct);
        await client.GetAsync("/api/xsrf-token", Ct);

        // The health request finished before the xsrf request was sent, so once the later span is exported the earlier one would be too.
        await isolated.Telemetry.WaitForSpanAsync(s => s.Kind == ActivityKind.Server && s.GetTagItem("url.path") as string == "/api/xsrf-token", Ct);
        isolated.Telemetry.Spans.ShouldNotContain(s => s.Kind == ActivityKind.Server && s.GetTagItem("url.path") as string == path);
    }

    [Fact]
    public void NoOtlpExporterWithoutEndpoint()
    {
        // Without OTEL_EXPORTER_OTLP_ENDPOINT nothing may leave the process: the OTLP options are never configured.
        var configured = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        configured.ShouldBeNullOrWhiteSpace("the test environment must not export");
        factory.Services.GetRequiredService<TracerProvider>().ShouldNotBeNull();
        factory.Services.GetServices<IConfigureOptions<OtlpExporterOptions>>().ShouldBeEmpty();
    }

    private static HttpClient Https(SlotBookApiFactory f) =>
        f.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
}
