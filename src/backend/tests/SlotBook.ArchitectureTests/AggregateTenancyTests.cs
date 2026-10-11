using Shouldly;
using Xunit;

namespace SlotBook.ArchitectureTests;

public sealed class AggregateTenancyTests
{
    [Fact]
    public void EveryAggregateIsTenantOwnedOrGlobal() =>
        ArchitectureRules.AggregateTenancyRule.Violations(SlotBookArchitecture.Production).ShouldBeEmpty();

    [Fact]
    public void AnAggregateThatIsNeitherOrBothFailsWithTheRuleName()
    {
        var violations = ArchitectureRules.AggregateTenancyRule.Violations(SlotBookArchitecture.Fixtures);

        violations.ShouldAllBe(violation => violation.StartsWith(ArchitectureRules.AggregateTenancy, StringComparison.Ordinal));
        violations.ShouldContain(violation => violation.Contains("UnmarkedAggregate", StringComparison.Ordinal));
        violations.ShouldContain(violation => violation.Contains("DoublyMarkedAggregate", StringComparison.Ordinal));
        foreach (var fine in new[] { "TenantOwnedIsFine", "GlobalIsFine", "AbstractBaseIsFine" })
        {
            violations.ShouldNotContain(violation => violation.Contains(fine, StringComparison.Ordinal));
        }
    }
}
