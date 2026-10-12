using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using Xunit;

namespace SlotBook.IntegrationTests.Security;

public class AuthenticationSchemeTests(SlotBookApiFactory factory) : IClassFixture<SlotBookApiFactory>
{
    [Theory]
    [InlineData("/test-only/security/patient")]
    [InlineData("/test-only/security/staff")]
    public async Task AnEndpointWithoutASessionAnswers401Unauthenticated(string path)
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = NewClient();

        var response = await client.GetAsync(path, ct);

        await ShouldBeUnauthenticatedAsync(response, ct);
    }

    [Fact]
    public async Task AStaffCookieDoesNotSatisfyAPatientEndpoint()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = NewClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/test-only/security/patient");
        request.Headers.Add("Cookie", "__Host-sb_staff=anything");

        await ShouldBeUnauthenticatedAsync(await client.SendAsync(request, ct), ct);
    }

    [Fact]
    public async Task APatientCookieDoesNotSatisfyAStaffEndpoint()
    {
        var ct = TestContext.Current.CancellationToken;
        using var client = NewClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/test-only/security/staff");
        request.Headers.Add("Cookie", "__Host-sb_patient=anything");

        await ShouldBeUnauthenticatedAsync(await client.SendAsync(request, ct), ct);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/api/health")]
    public async Task HealthEndpointsStayAnonymous(string path)
    {
        using var client = NewClient();

        (await client.GetAsync(path, TestContext.Current.CancellationToken)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static async Task ShouldBeUnauthenticatedAsync(HttpResponseMessage response, CancellationToken ct)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        body.GetProperty("code").GetString().ShouldBe("unauthenticated");
        body.GetProperty("traceId").GetString().ShouldNotBeNullOrEmpty();
    }

    private HttpClient NewClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
}
