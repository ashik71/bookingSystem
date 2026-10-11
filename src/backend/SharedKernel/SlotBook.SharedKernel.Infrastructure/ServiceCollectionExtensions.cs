using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SlotBook.SharedKernel.Application;
using SlotBook.SharedKernel.Infrastructure.Tenancy;
using SlotBook.SharedKernel.Infrastructure.Time;

namespace SlotBook.SharedKernel.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IClock"/> and the configured tenant. Startup fails when
    /// <c>Tenancy:Tenant</c> is missing or invalid. Needs a <see cref="TimeProvider"/> registered.
    /// </summary>
    public static IServiceCollection AddSharedKernel(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<IClock, Clock>();

        services.AddOptions<TenantOptions>()
            .Bind(configuration.GetSection(TenantOptions.SectionName))
            .ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<TenantOptions>, TenantOptionsValidator>());
        services.AddSingleton<ITenantContext, ConfiguredTenantContext>();

        return services;
    }
}
