using Shouldly;
using Xunit;

namespace SlotBook.IntegrationTests.Health;

public class HealthEndpointTests(SlotBookApiFactory factory) : IClassFixture<SlotBookApiFactory>
{
    [Theory]
    [InlineData("/health")]
    [InlineData("/api/health")]
    [InlineData("/health/ready")]
    public async Task HealthEndpointWithoutCredentialsReturns200JsonHealthy(string path)
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldBe("""{"status":"Healthy"}""");
    }
}
