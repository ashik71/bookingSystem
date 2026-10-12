using System.Diagnostics;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace SlotBook.IntegrationTests.Health;

public class HealthEndpointTests(SlotBookApiFactory factory) : IClassFixture<SlotBookApiFactory>
{
    private const string UnreachableMongo = "mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=500&connectTimeoutMS=500";
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";

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
    public async Task ReadyReportsConfiguredCommitSha()
    {
        using var withSha = new SlotBookApiFactory { Settings = new Dictionary<string, string?> { ["RENDER_GIT_COMMIT"] = Sha } };
        var client = withSha.CreateClient();

        var response = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldBe($$$"""{"status":"Healthy","commit":"{{{Sha}}}","checks":{"mongodb":"Healthy"}}""");
    }

    [Theory]
    [InlineData("<script>")]
    [InlineData("abc123")]
    [InlineData("ABCDEF1234567")]
    [InlineData("0123456789abcdef0123456789abcdef012345678")]
    public async Task ReadyOmitsCommitWhenValueIsNotASha(string value)
    {
        using var withValue = new SlotBookApiFactory { Settings = new Dictionary<string, string?> { ["RENDER_GIT_COMMIT"] = value } };
        var client = withValue.CreateClient();

        var response = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldBe("""{"status":"Healthy","checks":{"mongodb":"Healthy"}}""");
    }

    [Fact]
    public async Task ReadyIs503WithCommitShaWhenMongoDbUnreachable()
    {
        using var unreachable = new SlotBookApiFactory
        {
            MongoConnectionString = UnreachableMongo,
            SkipSchemaInitializer = true,
            Settings = new Dictionary<string, string?> { ["RENDER_GIT_COMMIT"] = Sha },
        };
        var client = unreachable.CreateClient();

        var response = await client.GetAsync("/health/ready", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.ServiceUnavailable);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldBe($$$"""{"status":"Unhealthy","commit":"{{{Sha}}}","checks":{"mongodb":"Unhealthy"}}""");
    }

    [Fact]
    public async Task LivenessDoesNotReportCommitSha()
    {
        using var withSha = new SlotBookApiFactory { Settings = new Dictionary<string, string?> { ["RENDER_GIT_COMMIT"] = Sha } };
        var client = withSha.CreateClient();

        var body = await client.GetStringAsync("/health", TestContext.Current.CancellationToken);

        body.ShouldBe("""{"status":"Healthy"}""");
    }

    [Fact]
    public async Task ReadyIs503UnhealthyWhenMongoDbUnreachable()
    {
        // Skipping the schema initializer simulates "MongoDB lost after startup".
        using var unreachable = new SlotBookApiFactory { MongoConnectionString = UnreachableMongo, SkipSchemaInitializer = true };
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
        // Skipping the schema initializer simulates "MongoDB lost after startup".
        using var unreachable = new SlotBookApiFactory { MongoConnectionString = UnreachableMongo, SkipSchemaInitializer = true };
        var client = unreachable.CreateClient();

        var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldBe("""{"status":"Healthy"}""");
    }

    [Fact]
    public void StartupFailsWhenMongoDbIsUnreachable()
    {
        using var unreachable = new SlotBookApiFactory { MongoConnectionString = UnreachableMongo };
        var clock = Stopwatch.StartNew();

        Should.Throw<Exception>(() => unreachable.CreateClient());

        clock.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(15));
    }

    [Fact]
    public void StartupFailsWhenMongoDbConnectionStringIsMissing()
    {
        using var missing = new SlotBookApiFactory { MongoConnectionString = null };

        var exception = Should.Throw<OptionsValidationException>(() => missing.CreateClient());

        exception.Message.ShouldContain("ConnectionStrings:mongodb");
    }
}
