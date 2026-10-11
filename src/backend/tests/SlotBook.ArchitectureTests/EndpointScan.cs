using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace SlotBook.ArchitectureTests;

/// <summary>Lists what every endpoint of the host binds from the route, the query string and headers.</summary>
internal static class EndpointScan
{
    public static IReadOnlyList<EndpointInput> Inputs(IServiceProvider services)
    {
        var inputs = new List<EndpointInput>();
        foreach (var endpoint in services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>())
        {
            var name = endpoint.DisplayName ?? endpoint.RoutePattern.RawText ?? "(unnamed)";
            inputs.AddRange(endpoint.RoutePattern.Parameters.Select(parameter => new EndpointInput(name, "route", parameter.Name)));

            if (endpoint.Metadata.GetMetadata<ControllerActionDescriptor>() is { } action)
            {
                inputs.AddRange(action.Parameters.SelectMany(parameter => ParameterInputs(name, parameter)));
                inputs.AddRange(action.BoundProperties.SelectMany(property => ParameterInputs(name, property)));
            }
        }

        return inputs;
    }

    private static IEnumerable<EndpointInput> ParameterInputs(string endpoint, ParameterDescriptor parameter)
    {
        var source = parameter.BindingInfo?.BindingSource;
        if (source == BindingSource.Body || source == BindingSource.Services || source == BindingSource.Special)
        {
            yield break;
        }

        var label = source == BindingSource.Header ? "header" : source == BindingSource.Path ? "route" : "query";
        yield return new EndpointInput(endpoint, label, parameter.BindingInfo?.BinderModelName ?? parameter.Name);

        // A model bound from the query or headers contributes one input per property.
        if (!IsSimple(parameter.ParameterType))
        {
            foreach (var property in parameter.ParameterType.GetProperties().Where(property => property.CanWrite))
            {
                yield return new EndpointInput(endpoint, label, property.Name);
            }
        }
    }

    private static bool IsSimple(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return underlying.IsPrimitive || underlying.IsEnum || underlying == typeof(string) || underlying == typeof(Guid)
            || underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset) || underlying == typeof(decimal)
            || underlying == typeof(DateOnly) || underlying == typeof(TimeOnly) || underlying == typeof(TimeSpan);
    }
}
