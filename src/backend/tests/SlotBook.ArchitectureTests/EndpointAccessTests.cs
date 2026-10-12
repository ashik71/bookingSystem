using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace SlotBook.ArchitectureTests;

public sealed class EndpointAccessTests
{
    public const string Rule = "AD-12 one scheme per endpoint";
    private static readonly string[] _schemes = ["patient", "staff"];

    [Fact]
    public void EveryEndpointDeclaresExactlyOneSchemeOrAnonymous()
    {
        using var host = new ScanHost(includeFixtureControllers: false);

        var endpoints = host.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>().ToList();
        endpoints.ShouldNotBeEmpty();
        Violations(endpoints).ShouldBeEmpty();
    }

    [Fact]
    public void EachKindOfViolationFailsWithTheRuleName()
    {
        using var host = new ScanHost(includeFixtureControllers: true);

        var endpoints = host.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("fixture/access", StringComparison.Ordinal) ?? false)
            .ToList();
        var violations = Violations(endpoints);

        violations.ShouldAllBe(violation => violation.StartsWith(Rule, StringComparison.Ordinal));
        violations.ShouldContain(violation => violation.Contains("fixture/access/none", StringComparison.Ordinal) && violation.Contains("declares no scheme", StringComparison.Ordinal));
        violations.ShouldContain(violation => violation.Contains("fixture/access/authorize-without-scheme", StringComparison.Ordinal) && violation.Contains("declares no scheme", StringComparison.Ordinal));
        violations.ShouldContain(violation => violation.Contains("fixture/access/both", StringComparison.Ordinal) && violation.Contains("more than one scheme", StringComparison.Ordinal));
        violations.ShouldContain(violation => violation.Contains("fixture/access/anonymous-and-scheme", StringComparison.Ordinal) && violation.Contains("anonymous but also", StringComparison.Ordinal));
        violations.ShouldContain(violation => violation.Contains("fixture/access/staff-without-roles", StringComparison.Ordinal) && violation.Contains("no roles", StringComparison.Ordinal));
        violations.ShouldContain(violation => violation.Contains("fixture/access/unknown-scheme", StringComparison.Ordinal) && violation.Contains("unknown scheme 'admin'", StringComparison.Ordinal));
        violations.ShouldNotContain(violation => violation.Contains("valid-", StringComparison.Ordinal));
    }

    private static List<string> Violations(IEnumerable<RouteEndpoint> endpoints)
    {
        var violations = new List<string>();
        foreach (var endpoint in endpoints)
        {
            var name = $"{endpoint.DisplayName ?? "(unnamed)"} [{endpoint.RoutePattern.RawText}]";
            var data = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();
            var anonymous = endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null;
            var declared = data
                .SelectMany(item => (item.AuthenticationSchemes ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (anonymous)
            {
                if (data.Count > 0)
                {
                    violations.Add($"{Rule}: {name} is anonymous but also declares authorization");
                }

                continue;
            }

            if (declared.Count == 0)
            {
                violations.Add($"{Rule}: {name} declares no scheme and is not anonymous");
                continue;
            }

            if (declared.Count > 1)
            {
                violations.Add($"{Rule}: {name} declares more than one scheme ({string.Join(", ", declared)})");
                continue;
            }

            if (!_schemes.Contains(declared[0], StringComparer.Ordinal))
            {
                violations.Add($"{Rule}: {name} names unknown scheme '{declared[0]}'");
            }
            else if (declared[0] == "staff" && !data.Any(item => !string.IsNullOrWhiteSpace(item.Roles)))
            {
                violations.Add($"{Rule}: {name} is a staff endpoint with no roles");
            }
        }

        return violations;
    }
}
