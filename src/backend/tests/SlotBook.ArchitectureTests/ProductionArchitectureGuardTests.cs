using ArchUnitNET.Domain;
using ArchUnitNET.Domain.Dependencies;
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

    [Fact]
    public void TheLoadedArchitectureHoldsTheAggregateBasesAndTheCodeThatAsksForCollections()
    {
        var types = SlotBookArchitecture.Production.Types.Select(type => type.FullName).ToList();

        types.ShouldContain("SlotBook.SharedKernel.Domain.AggregateRoot");
        types.ShouldContain("SlotBook.SharedKernel.Domain.TenantAggregateRoot");
        types.ShouldContain("SlotBook.SharedKernel.Domain.GlobalAggregateAttribute");

        // The no-unfiltered-collection rule would pass vacuously if the one allowed caller were not seen calling.
        SlotBookArchitecture.Production.Types
            .Where(type => type.FullName.StartsWith("SlotBook.SharedKernel.Infrastructure.Persistence.TenantCollection", StringComparison.Ordinal))
            .SelectMany(type => type.Dependencies.OfType<MethodCallDependency>())
            .ShouldContain(call => call.TargetMember.Name.StartsWith("GetCollection", StringComparison.Ordinal));
    }
}
