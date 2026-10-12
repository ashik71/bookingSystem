using System.Net.Http.Json;
using System.Text;
using Shouldly;
using Xunit;

namespace SlotBook.IntegrationTests.Observability;

/// <summary>SC-6 / AD-17: no personal data in any span, metric tag or log record.</summary>
public class PersonalDataScanTests
{
    private const string Mobile = "+44700900314";
    private const string Otp = "271828";
    private const string Session = "sess-9f3b1c7e5a2d4c80b6e1";
    private const string Name = "Zebulon-Quillfeather";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task SentinelValuesNeverReachSpansMetricsOrLogs()
    {
        using var factory = new SlotBookApiFactory();
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        string[] sentinels = [Mobile, Otp, Session, Name, "44700900314"];

        // query string, JSON body (a MongoDB write), cookie, a validation failure and a server error
        using var query = new HttpRequestMessage(HttpMethod.Get, $"/test-only/refuse?mobile={Uri.EscapeDataString(Mobile)}&otp={Otp}");
        query.Headers.Add("Cookie", $"session={Session}");
        await client.SendAsync(query, Ct);

        using var write = new HttpRequestMessage(HttpMethod.Post, $"/test-only/probes?token={Session}")
        {
            Content = JsonContent.Create(new { name = Name, mobile = Mobile, otp = Otp }),
        };
        write.Headers.Add("Cookie", $"session={Session}");
        await client.SendAsync(write, Ct);

        using var invalid = new HttpRequestMessage(HttpMethod.Post, "/test-only/probes")
        {
            Content = new StringContent($"{{\"name\":\"{Name}\",\"mobile\":{Mobile},\"otp\":\"{Otp}\"", Encoding.UTF8, "application/json"),
        };
        await client.SendAsync(invalid, Ct);

        using var failing = new HttpRequestMessage(HttpMethod.Get, $"/test-only/throw?mobile={Uri.EscapeDataString(Mobile)}");
        failing.Headers.Add("Cookie", $"session={Session}");
        await client.SendAsync(failing, Ct);

        var telemetry = factory.Telemetry;
        telemetry.Spans.ShouldNotBeEmpty();
        telemetry.Spans.ShouldContain(s => s.Source.Name == "MongoDB.Driver"); // the sentinels did reach MongoDB commands
        var spanTexts = telemetry.SpanTexts();
        var metricTags = telemetry.CollectMetricTags(factory.Services);
        var logs = telemetry.LogRecords.Concat(factory.Logs.Records.Select(r => r.Message)).ToList();
        logs.ShouldNotBeEmpty();

        foreach (var sentinel in sentinels)
        {
            spanTexts.ShouldNotContain(t => t.Contains(sentinel, StringComparison.OrdinalIgnoreCase), sentinel);
            metricTags.ShouldNotContain(t => t.Contains(sentinel, StringComparison.OrdinalIgnoreCase), sentinel);
            logs.ShouldNotContain(t => t.Contains(sentinel, StringComparison.OrdinalIgnoreCase), sentinel);
        }

        foreach (var span in telemetry.Spans)
        {
            var keys = span.TagObjects.Select(t => t.Key).ToList();
            keys.ShouldNotContain("url.query");
            keys.ShouldNotContain("db.query.text");
            keys.ShouldNotContain("db.statement");
        }
    }
}
