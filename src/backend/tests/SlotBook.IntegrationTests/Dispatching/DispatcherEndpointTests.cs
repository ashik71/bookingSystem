using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SlotBook.IntegrationTests.TestSupport;
using Xunit;

namespace SlotBook.IntegrationTests.Dispatching;

public sealed class DispatcherEndpointTests(SlotBookApiFactory factory) : IClassFixture<SlotBookApiFactory>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task InvalidFieldReturns400ValidationFailedWithFieldCodesAndHandlerNeverRuns()
    {
        var probe = factory.Services.GetRequiredService<HandlerProbe>();
        probe.Reset();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/test-only/probes", new { displayName = string.Empty, count = 0 }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        using var body = await ReadAsync(response);
        body.RootElement.GetProperty("code").GetString().ShouldBe("validation_failed");
        body.RootElement.GetProperty("traceId").GetString()!.ShouldMatch("^[0-9a-f]{32}$");
        var errors = body.RootElement.GetProperty("errors");
        errors.GetProperty("displayName").EnumerateArray().Select(e => e.GetString()).ShouldBe(["required"]);
        errors.GetProperty("count").EnumerateArray().Select(e => e.GetString()).ShouldBe(["out_of_range"]);
        probe.Runs.ShouldBe(0);
    }

    [Fact]
    public async Task MissingNonNullableFieldIsReportedByTheValidatorNotModelBinding()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/test-only/probes", new { count = 3 }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var text = await response.Content.ReadAsStringAsync(Ct);
        text.ShouldNotContain("field is required");
        using var body = JsonDocument.Parse(text);
        body.RootElement.GetProperty("errors").GetProperty("displayName").EnumerateArray()
            .Select(e => e.GetString()).ShouldBe(["required"]);
    }

    [Fact]
    public async Task ValidCommandRunsHandlerOnceAndReturns200()
    {
        var probe = factory.Services.GetRequiredService<HandlerProbe>();
        probe.Reset();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/test-only/probes", new { displayName = "Ada", count = 2 }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var body = await ReadAsync(response);
        body.RootElement.GetProperty("displayName").GetString().ShouldBe("Ada");
        probe.Runs.ShouldBe(1);
    }

    [Fact]
    public async Task RefusedResultReturns422WithItsCode()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/test-only/probes/refuse", new { code = "probe_refused" }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        using var body = await ReadAsync(response);
        body.RootElement.GetProperty("code").GetString().ShouldBe("probe_refused");
        body.RootElement.GetProperty("traceId").GetString()!.ShouldMatch("^[0-9a-f]{32}$");
        body.RootElement.TryGetProperty("errors", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task RefusedNotFoundResultReturns404()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/test-only/probes/refuse", new { code = "not_found" }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        using var body = await ReadAsync(response);
        body.RootElement.GetProperty("code").GetString().ShouldBe("not_found");
        body.RootElement.GetProperty("traceId").GetString().ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task DomainExceptionNotFoundReturns404AndOtherCodesReturn422()
    {
        var client = factory.CreateClient();

        var notFound = await client.GetAsync("/test-only/probes/throw-domain/not_found", Ct);
        var other = await client.GetAsync("/test-only/probes/throw-domain/slot_full", Ct);

        notFound.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        other.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task WrongJsonTypeReturns400WithInvalidFormatAndNoFrameworkText()
    {
        var client = factory.CreateClient();
        using var content = new StringContent("""{"displayName":"Ada","count":"many"}""", Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/test-only/probes", content, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var text = await response.Content.ReadAsStringAsync(Ct);
        text.ShouldNotContain("could not be converted");
        using var body = JsonDocument.Parse(text);
        body.RootElement.GetProperty("code").GetString().ShouldBe("validation_failed");
        body.RootElement.GetProperty("errors").GetProperty("count").EnumerateArray()
            .Select(e => e.GetString()).ShouldBe(["invalid_format"]);
    }

    [Fact]
    public async Task MalformedJsonReturns400WithInvalidFormatOnBody()
    {
        var client = factory.CreateClient();
        using var content = new StringContent("{not json", Encoding.UTF8, "application/json");

        var response = await client.PostAsync("/test-only/probes", content, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        using var body = await ReadAsync(response);
        body.RootElement.GetProperty("code").GetString().ShouldBe("validation_failed");
        body.RootElement.GetProperty("errors").GetProperty("body").EnumerateArray()
            .Select(e => e.GetString()).ShouldBe(["invalid_format"]);
    }

    private static async Task<JsonDocument> ReadAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
}
