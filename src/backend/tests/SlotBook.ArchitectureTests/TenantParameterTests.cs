using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace SlotBook.ArchitectureTests;

public sealed class TenantParameterTests
{
    public const string Rule = "AD-5 no tenant parameters";

    [Fact]
    public void TheApiHasNoTenantParameters()
    {
        using var host = new ScanHost(includeFixtureControllers: false);

        // The health endpoints take no parameters, so the scan finds nothing yet; the fixture test below proves it bites.
        host.Services.GetRequiredService<EndpointDataSource>().Endpoints.ShouldNotBeEmpty();
        Violations(EndpointScan.Inputs(host.Services)).ShouldBeEmpty();
    }

    [Fact]
    public void ATenantInTheRouteTheQueryOrAHeaderFailsWithTheRuleName()
    {
        using var host = new ScanHost(includeFixtureControllers: true);

        var violations = Violations(EndpointScan.Inputs(host.Services));

        violations.ShouldAllBe(violation => violation.StartsWith(Rule, StringComparison.Ordinal));
        violations.ShouldContain(violation => violation.Contains("route parameter 'tenantId'", StringComparison.Ordinal));
        violations.ShouldContain(violation => violation.Contains("query parameter 'tenant'", StringComparison.Ordinal));
        violations.ShouldContain(violation => violation.Contains("header parameter 'X-Tenant'", StringComparison.Ordinal));
    }

    private static List<string> Violations(IEnumerable<EndpointInput> inputs) =>
        [.. inputs
            .Where(input => input.Name.Contains("tenant", StringComparison.OrdinalIgnoreCase))
            .Select(input => $"{Rule}: {input.Endpoint} has the {input.Source} parameter '{input.Name}'")];
}
