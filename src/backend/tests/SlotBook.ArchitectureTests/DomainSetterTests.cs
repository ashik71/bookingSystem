using Shouldly;
using Xunit;

namespace SlotBook.ArchitectureTests;

public sealed class DomainSetterTests
{
    [Fact]
    public void TheDomainCodeHasNoPublicSetters() =>
        ArchitectureRules.NoPublicSettersRule.Violations(SlotBookArchitecture.Production).ShouldBeEmpty();

    [Fact]
    public void SettersVisibleOutsideTheTypeFailWithTheRuleName()
    {
        var violations = ArchitectureRules.NoPublicSettersRule.Violations(SlotBookArchitecture.Fixtures);

        violations.ShouldAllBe(violation => violation.StartsWith(ArchitectureRules.NoPublicSetters, StringComparison.Ordinal));
        foreach (var property in new[] { "PublicSet", "PublicInit", "InternalSet", "ProtectedSet" })
        {
            violations.ShouldContain(violation => violation.Contains(property + " ", StringComparison.Ordinal) || violation.Contains("::" + property, StringComparison.Ordinal));
        }

        violations.ShouldNotContain(violation => violation.Contains("PrivateSetIsFine", StringComparison.Ordinal));
        violations.ShouldNotContain(violation => violation.Contains("NoSetterIsFine", StringComparison.Ordinal));
    }
}
