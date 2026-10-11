using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;
using SlotBook.SharedKernel.Application;
using SlotBook.SharedKernel.Domain;
using Xunit;

namespace SlotBook.IntegrationTests.Tenancy;

public class ConfiguredTenantTests
{
    [Fact]
    public void StartupFailsWhenTenancyTenantIsMissing()
    {
        using var factory = new SlotBookApiFactory { TenantSlug = null };

        var exception = Should.Throw<OptionsValidationException>(() => factory.CreateClient());

        exception.Message.ShouldContain("Tenancy:Tenant");
    }

    [Fact]
    public void TenantContextReturnsIdDerivedFromConfiguredSlug()
    {
        using var factory = new SlotBookApiFactory();

        var tenantId = factory.Services.GetRequiredService<ITenantContext>().TenantId;

        tenantId.ShouldBe(DeterministicId.From(DeterministicId.SlotBookNamespace, "tenant:test-clinic"));
        tenantId.ShouldBe(Guid.Parse("e730b082-ab61-5f0b-9aa9-e7053a95f06a"));
    }

    [Fact]
    public void TenantContextDiffersPerSlug()
    {
        using var factory = new SlotBookApiFactory { TenantSlug = "other-clinic" };

        var tenantId = factory.Services.GetRequiredService<ITenantContext>().TenantId;

        tenantId.ShouldNotBe(DeterministicId.ForTenant("test-clinic"));
    }
}
