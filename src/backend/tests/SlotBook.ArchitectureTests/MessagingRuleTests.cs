using Shouldly;
using Xunit;

namespace SlotBook.ArchitectureTests;

public sealed class MessagingRuleTests
{
    [Theory]
    [InlineData(ArchitectureRules.IntegrationEvents)]
    [InlineData(ArchitectureRules.ConsumerDefinitions)]
    public void TheCodeFollowsTheRule(string ruleName) =>
        ArchitectureRules.ByName(ruleName).Violations(SlotBookArchitecture.Production).ShouldBeEmpty();

    [Fact]
    public void TheLoadedArchitectureHoldsTheBasesTheMessagingRulesDependOn()
    {
        var types = SlotBookArchitecture.Production.Types.Select(type => type.FullName).ToList();

        types.ShouldContain("SlotBook.SharedKernel.Contracts.TenantEvent");
        types.ShouldContain("SlotBook.SharedKernel.Infrastructure.Messaging.SlotBookConsumerDefinition`1");
    }

    [Fact]
    public void AnUnsealedEventOrOneWithAnEventSuffixFails()
    {
        var violations = ArchitectureRules.IntegrationEventsRule.Violations(SlotBookArchitecture.Fixtures);

        violations.ShouldAllBe(violation => violation.StartsWith(ArchitectureRules.IntegrationEvents, StringComparison.Ordinal));
        violations.ShouldContain(violation => violation.Contains("FixtureUnsealed", StringComparison.Ordinal));
        violations.ShouldContain(violation => violation.Contains("FixtureNoteMovedEvent", StringComparison.Ordinal));
        violations.ShouldNotContain(violation => violation.Contains("FixtureNoteCreated", StringComparison.Ordinal));
    }

    [Fact]
    public void AConsumerWithoutADefinitionFails()
    {
        var violations = ArchitectureRules.ConsumerDefinitionsRule.Violations(SlotBookArchitecture.Fixtures);

        violations.ShouldAllBe(violation => violation.StartsWith(ArchitectureRules.ConsumerDefinitions, StringComparison.Ordinal));
        violations.ShouldContain(violation => violation.Contains("FixtureOrphanConsumer", StringComparison.Ordinal));
        violations.ShouldNotContain(violation => violation.Contains("FixtureDefinedConsumer", StringComparison.Ordinal));
    }
}
