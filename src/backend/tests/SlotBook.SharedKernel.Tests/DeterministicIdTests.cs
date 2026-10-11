using Shouldly;
using SlotBook.SharedKernel.Domain;
using Xunit;

namespace SlotBook.SharedKernel.Tests;

public class DeterministicIdTests
{
    private static readonly Guid _dnsNamespace = Guid.Parse("6ba7b810-9dad-11d1-80b4-00c04fd430c8");

    [Fact]
    public void MatchesRfc9562AppendixVector()
    {
        DeterministicId.From(_dnsNamespace, "www.example.com")
            .ShouldBe(Guid.Parse("2ed6657d-e927-568b-95e1-2665a8aea6a2"));
    }

    [Fact]
    public void SetsVersion5AndRfcVariant()
    {
        var text = DeterministicId.ForTenant("some-clinic").ToString();

        text[14].ShouldBe('5');
        text[19].ShouldBeOneOf('8', '9', 'a', 'b');
    }

    [Fact]
    public void SameInputGivesSameIdAndDifferentNameGivesDifferentId()
    {
        DeterministicId.ForTenant("a").ShouldBe(DeterministicId.ForTenant("a"));
        DeterministicId.ForTenant("a").ShouldNotBe(DeterministicId.ForTenant("b"));
    }

    [Fact]
    public void GuardIdIsStableAndDiffersByTenantKindAndKey()
    {
        var tenant = DeterministicId.ForTenant("a");

        DeterministicId.ForGuard(tenant, "counter", "x").ShouldBe(DeterministicId.ForGuard(tenant, "counter", "x"));
        DeterministicId.ForGuard(tenant, "counter", "x").ShouldNotBe(DeterministicId.ForGuard(DeterministicId.ForTenant("b"), "counter", "x"));
        DeterministicId.ForGuard(tenant, "counter", "x").ShouldNotBe(DeterministicId.ForGuard(tenant, "limit", "x"));
        DeterministicId.ForGuard(tenant, "counter", "x").ShouldNotBe(DeterministicId.ForGuard(tenant, "counter", "y"));
    }

    [Fact]
    public void GuardIdKeyPartsCannotBeShiftedAcrossTheSeparator()
    {
        var tenant = DeterministicId.ForTenant("a");

        DeterministicId.ForGuard(tenant, "k", "a:1:b").ShouldNotBe(DeterministicId.ForGuard(tenant, "k", "a", "b"));
        DeterministicId.ForGuard(tenant, "k", "ab", "c").ShouldNotBe(DeterministicId.ForGuard(tenant, "k", "a", "bc"));
    }
}
