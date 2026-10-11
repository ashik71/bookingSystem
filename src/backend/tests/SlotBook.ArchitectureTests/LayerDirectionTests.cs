using Shouldly;
using Xunit;

namespace SlotBook.ArchitectureTests;

/// <remarks>An empty result on the real code is fine: a layer may have no code yet.</remarks>
public sealed class LayerDirectionTests
{
    [Theory]
    [InlineData(ArchitectureRules.DomainLayer)]
    [InlineData(ArchitectureRules.ApplicationLayer)]
    [InlineData(ArchitectureRules.ApiLayer)]
    public void TheCodeFollowsTheRule(string ruleName) =>
        ArchitectureRules.ByName(ruleName).Violations(SlotBookArchitecture.Production).ShouldBeEmpty();

    [Theory]
    [InlineData(ArchitectureRules.DomainLayer, "FixtureApplicationType", "FixtureInfrastructureType", "ObjectId")]
    [InlineData(ArchitectureRules.ApplicationLayer, "FixtureInfrastructureType", "ObjectId")]
    [InlineData(ArchitectureRules.ApiLayer, "ObjectId")]
    public void AViolationFailsWithTheRuleName(string ruleName, params string[] offendingTypes)
    {
        var violations = ArchitectureRules.ByName(ruleName).Violations(SlotBookArchitecture.Fixtures);

        violations.ShouldNotBeEmpty();
        violations.ShouldAllBe(violation => violation.StartsWith(ruleName, StringComparison.Ordinal));
        foreach (var offender in offendingTypes)
        {
            violations.ShouldContain(violation => violation.Contains(offender, StringComparison.Ordinal));
        }
    }
}
