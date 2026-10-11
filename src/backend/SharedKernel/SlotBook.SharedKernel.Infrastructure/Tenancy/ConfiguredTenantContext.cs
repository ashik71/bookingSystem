using Microsoft.Extensions.Options;
using SlotBook.SharedKernel.Application;
using SlotBook.SharedKernel.Domain;

namespace SlotBook.SharedKernel.Infrastructure.Tenancy;

internal sealed class ConfiguredTenantContext(IOptions<TenantOptions> options) : ITenantContext
{
    public Guid TenantId { get; } = DeterministicId.ForTenant(options.Value.Slug!);
}
