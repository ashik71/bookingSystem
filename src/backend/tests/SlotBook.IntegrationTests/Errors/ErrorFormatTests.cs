using System.Net;
using System.Text.Json;
using Shouldly;
using Xunit;

namespace SlotBook.IntegrationTests.Errors;

public class ErrorFormatTests(SlotBookApiFactory factory) : IClassFixture<SlotBookApiFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task UnhandledExceptionReturnsInternalErrorProblemWithTraceIdAndMatchingLogLine()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/test-only/throw", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        var text = await response.Content.ReadAsStringAsync(Ct);
        text.ShouldNotContain("secret internal detail");
        using var body = JsonDocument.Parse(text);
        body.RootElement.GetProperty("status").GetInt32().ShouldBe(500);
        body.RootElement.GetProperty("code").GetString().ShouldBe("internal_error");
        var traceId = body.RootElement.GetProperty("traceId").GetString();
        traceId.ShouldNotBeNull();
        traceId.ShouldMatch("^[0-9a-f]{32}$");
        factory.Logs.Records.ShouldContain(r => r.EventId == 1 && r.TraceId == traceId);
    }

    [Fact]
    public async Task IncomingTraceparentTraceIdIsUsed()
    {
        var client = factory.CreateClient();
        const string traceId = "4bf92f3577b34da6a3ce929d0e0e4736";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/test-only/throw");
        request.Headers.Add("traceparent", $"00-{traceId}-00f067aa0ba902b7-01");

        var response = await client.SendAsync(request, Ct);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        body.RootElement.GetProperty("traceId").GetString().ShouldBe(traceId);
        factory.Logs.Records.ShouldContain(r => r.EventId == 1 && r.TraceId == traceId);
    }

    [Fact]
    public async Task EveryLogLineDuringARequestCarriesItsTraceIdAndRequestIsLogged()
    {
        var client = factory.CreateClient();
        const string traceId = "0af7651916cd43dd8448eb211c80319c";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/test-only/throw?secret=1");
        request.Headers.Add("traceparent", $"00-{traceId}-b7ad6b7169203331-01");

        await client.SendAsync(request, Ct);

        var lines = factory.Logs.Records.Where(r => r.TraceId == traceId).ToList();
        lines.ShouldNotBeEmpty();
        var requestLine = lines.Single(r => r.EventId == 2);
        requestLine.Message.ShouldBe("GET /test-only/throw responded 500");
    }

    [Fact]
    public async Task DomainExceptionReturns422WithItsCode()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/test-only/refuse", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        body.RootElement.GetProperty("code").GetString().ShouldBe("slot_full");
        body.RootElement.GetProperty("traceId").GetString().ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task UnknownRouteReturns404NotFoundProblem()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/no-such-route", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        body.RootElement.GetProperty("code").GetString().ShouldBe("not_found");
        body.RootElement.GetProperty("traceId").GetString().ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task WrongMethodReturns400ValidationFailedProblem()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/test-only/post-only", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        body.RootElement.GetProperty("code").GetString().ShouldBe("validation_failed");
    }
}
