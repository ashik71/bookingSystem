using Shouldly;
using Xunit;

namespace SlotBook.ArchitectureTests;

public sealed class ProductionArchitectureGuardTests
{
    [Fact]
    public void TheLoadedArchitectureHoldsTheAssembliesTheRulesDependOn()
    {
        var assemblies = SlotBookArchitecture.Production.Assemblies.Select(assembly => assembly.Name).ToList();

        assemblies.ShouldContain("SlotBook.Api");
        assemblies.ShouldContain("SlotBook.SharedKernel.Domain");
        assemblies.ShouldContain("SlotBook.SharedKernel.Application");
        assemblies.ShouldContain("SlotBook.SharedKernel.Infrastructure");
    }

    [Fact]
    public void TheLoadedArchitectureHasTypesInADomainNamespace() =>
        SlotBookArchitecture.Production.Types
            .ShouldContain(type => type.Namespace.FullName.EndsWith(".Domain", StringComparison.Ordinal));
}
