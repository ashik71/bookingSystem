using Shouldly;
using Xunit;

namespace SlotBook.IntegrationTests.Security;

public class SecurityHeadersTests(SlotBookApiFactory factory) : IClassFixture<SlotBookApiFactory>
{
    [Theory]
    [InlineData("/health", 200)]
    [InlineData("/api/health", 200)]
    [InlineData("/no/such/route", 404)]
    [InlineData("/test-only/throw", 500)]
    [InlineData("/test-only/refuse", null)]
    public async Task EveryResponseCarriesTheCspAndHsts(string path, int? expectedStatus)
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        if (expectedStatus is { } status)
        {
            ((int)response.StatusCode).ShouldBe(status);
        }

        response.Headers.GetValues("Content-Security-Policy").ShouldBe(["default-src 'none'"]);
        response.Headers.GetValues("Strict-Transport-Security").ShouldBe(["max-age=31536000"]);
    }
}
