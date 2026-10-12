using System.Diagnostics;
using Shouldly;
using SlotBook.ServiceDefaults.Telemetry;
using Xunit;

namespace SlotBook.IntegrationTests.Observability;

public class SensitiveTagProcessorTests
{
    [Fact]
    public void RemovesForbiddenTagsAndKeepsTheRest()
    {
        using var activity = new Activity("test");
        activity.SetTag("url.query", "phone=+44700900123");
        activity.SetTag("db.query.text", "{ find: 'x' }");
        activity.SetTag("db.statement", "select");
        activity.SetTag("http.request.body", "{}");
        activity.SetTag("http.route", "api/x");
        activity.SetTag("http.response.status_code", 200);

        using var processor = new SensitiveTagProcessor();
        processor.OnEnd(activity);

        activity.TagObjects.Select(t => t.Key).ShouldBe(["http.route", "http.response.status_code"], ignoreOrder: true);
    }
}
