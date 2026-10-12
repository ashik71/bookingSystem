using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using Xunit;

namespace SlotBook.IntegrationTests.Security;

public class AntiforgeryTests(SlotBookApiFactory factory) : IClassFixture<SlotBookApiFactory>
{
    private const string Probe = "/test-only/security/protected";

    [Fact]
    public async Task AnUnsafeRequestWithoutTheHeaderIsRefusedWithXsrfInvalid()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = NewClient();
        (await client.GetAsync("/api/xsrf-token", ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var response = await client.PostAsync(Probe, null, ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        body.GetProperty("code").GetString().ShouldBe("xsrf_invalid");
    }

    [Fact]
    public async Task AMismatchedHeaderIsRefused()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = NewClient();
        await client.GetAsync("/api/xsrf-token", ct);
        using var request = new HttpRequestMessage(HttpMethod.Post, Probe);
        request.Headers.Add("X-XSRF-TOKEN", "not-the-token");

        var response = await client.SendAsync(request, ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>(ct)).GetProperty("code").GetString().ShouldBe("xsrf_invalid");
    }

    [Fact]
    public async Task AValidCookieAndHeaderPairIsAccepted()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = NewClient();
        var issue = await client.GetAsync("/api/xsrf-token", ct);
        var token = XsrfCookies.RequestToken(issue);
        using var request = new HttpRequestMessage(HttpMethod.Post, Probe);
        request.Headers.Add("X-XSRF-TOKEN", token);

        (await client.SendAsync(request, ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task TheIssuedCookiesCarryTheAgreedFlags()
    {
        var issue = await NewClient().GetAsync("/api/xsrf-token", TestContext.Current.CancellationToken);

        var cookies = issue.Headers.GetValues("Set-Cookie").ToList();
        var secret = cookies.Single(c => c.StartsWith("__Host-sb_xsrf=", StringComparison.Ordinal));
        secret.ShouldContain("httponly", Case.Insensitive);
        secret.ShouldContain("secure", Case.Insensitive);
        secret.ShouldContain("samesite=strict", Case.Insensitive);
        var readable = cookies.Single(c => c.StartsWith("XSRF-TOKEN=", StringComparison.Ordinal));
        readable.ShouldNotContain("httponly", Case.Insensitive);
        readable.ShouldContain("secure", Case.Insensitive);
        readable.ShouldContain("samesite=strict", Case.Insensitive);
    }

    [Fact]
    public async Task ASafeRequestNeedsNoToken()
    {
        using var client = NewClient();

        (await client.GetAsync("/test-only/security/open", TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private HttpClient NewClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
}
