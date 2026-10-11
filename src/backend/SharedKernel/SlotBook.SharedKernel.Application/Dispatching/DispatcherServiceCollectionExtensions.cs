using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SlotBook.SharedKernel.Application.Persistence;

namespace SlotBook.SharedKernel.Application.Dispatching;

/// <summary>Registers the dispatcher and scans modules for handlers and validators (AD-4).</summary>
public static class DispatcherServiceCollectionExtensions
{
    /// <summary>Registers <see cref="IDispatcher"/>. Call once.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddDispatcher(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddScoped<IDispatcher, Dispatcher>();
        services.TryAddScoped(typeof(CommandPipeline<,>));
        services.TryAddScoped(typeof(QueryPipeline<,>));
        services.TryAddSingleton<ICommandExecution, PassThroughCommandExecution>();
        return services;
    }

    /// <summary>Registers every command handler, query handler and validator in <paramref name="assembly"/>, including internal types.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">The module assembly to scan.</param>
    /// <returns>The same collection.</returns>
    public static IServiceCollection AddUseCasesFrom(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        foreach (var type in assembly.GetTypes())
        {
            if (type is not { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            {
                continue;
            }

            foreach (var service in type.GetInterfaces().Where(IsUseCaseService))
            {
                services.TryAddEnumerable(ServiceDescriptor.Scoped(service, type));
            }
        }

        return services;
    }

    private static bool IsUseCaseService(Type type) =>
        type.IsGenericType &&
        type.GetGenericTypeDefinition() is var definition &&
        (definition == typeof(ICommandHandler<,>) || definition == typeof(IQueryHandler<,>) || definition == typeof(IValidator<>));
}
