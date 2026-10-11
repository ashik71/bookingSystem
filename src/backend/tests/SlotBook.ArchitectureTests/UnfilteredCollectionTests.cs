using Shouldly;
using Xunit;

namespace SlotBook.ArchitectureTests;

public sealed class UnfilteredCollectionTests
{
    [Fact]
    public void OnlyThePersistenceTypesOfTheSharedKernelAskForACollection() =>
        ArchitectureRules.NoUnfilteredCollectionRule.Violations(SlotBookArchitecture.Production).ShouldBeEmpty();

    [Fact]
    public void AskingTheDatabaseForACollectionOutsidePersistenceFailsWithTheRuleName()
    {
        var violations = ArchitectureRules.NoUnfilteredCollectionRule.Violations(SlotBookArchitecture.Fixtures);

        violations.ShouldAllBe(violation => violation.StartsWith(ArchitectureRules.NoUnfilteredCollection, StringComparison.Ordinal));
        violations.ShouldContain(violation => violation.Contains("UnfilteredCollectionViolation", StringComparison.Ordinal));
        violations.ShouldNotContain(violation => violation.Contains("DatabaseOnlyIsFine", StringComparison.Ordinal));
    }
}
