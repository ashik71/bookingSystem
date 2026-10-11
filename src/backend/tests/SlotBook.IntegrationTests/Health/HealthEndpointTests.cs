using System.Diagnostics;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace SlotBook.IntegrationTests.Health;

public class HealthEndpointTests(SlotBookApiFactory factory) : IClassFixture<SlotBookApiFactory>
{
    private const string UnreachableMongo = "mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=500&connectTimeoutMS=500";

    [Theory]
    [InlineData("/health")]
    [InlineData("/api/health")]
    public async Task LivenessWithoutCredentialsReturns200JsonHealthy(string path)
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldBe("""{"status":"Healthy"}""");
    }

    [Fact]
    public async Task ReadyReportsMongoDbHealthyWhenReachable()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldBe("""{"status":"Healthy","checks":{"mongodb":"Healthy"}}""");
    }

    [Fact]
    public async Task ReadyIs503UnhealthyWhenMongoDbUnreachable()
    {
        using var unreachable = new SlotBookApiFactory { MongoConnectionString = UnreachableMongo };
        var client = unreachable.CreateClient();
        var clock = Stopwatch.StartNew();

        var response = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);

        clock.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(10));
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.ServiceUnavailable);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldBe("""{"status":"Unhealthy","checks":{"mongodb":"Unhealthy"}}""");
        body.ShouldNotContain("127.0.0.1");
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/api/health")]
    public async Task LivenessStaysHealthyWhenMongoDbUnreachable(string path)
    {
        using var unreachable = new SlotBookApiFactory { MongoConnectionString = UnreachableMongo };
        var client = unreachable.CreateClient();

        var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldBe("""{"status":"Healthy"}""");
    }

    [Fact]
    public void StartupFailsWhenMongoDbConnectionStringIsMissing()
    {
        using var missing = new SlotBookApiFactory { MongoConnectionString = null };

        var exception = Should.Throw<OptionsValidationException>(() => missing.CreateClient());

        exception.Message.ShouldContain("ConnectionStrings:mongodb");
    }
}
