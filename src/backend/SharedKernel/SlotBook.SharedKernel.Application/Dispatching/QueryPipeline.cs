using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SlotBook.SharedKernel.Contracts;

namespace SlotBook.SharedKernel.Application.Dispatching;

#pragma warning disable CA1812 // instantiated by dependency injection

internal sealed class QueryPipeline<TQuery, TResponse>(IServiceProvider services)
    : Dispatcher.QueryPipelineBase<TResponse>
    where TQuery : IQuery<TResponse>
{
    public override async Task<TResponse> RunAsync(IQuery<TResponse> query, CancellationToken cancellationToken)
    {
        var typed = (TQuery)query;
        await Dispatcher.ValidateAsync(services.GetServices<IValidator<TQuery>>(), typed, cancellationToken).ConfigureAwait(false);
        var handler = services.GetService<IQueryHandler<TQuery, TResponse>>()
            ?? throw new InvalidOperationException($"No query handler is registered for {typeof(TQuery)}.");
        return await handler.HandleAsync(typed, cancellationToken).ConfigureAwait(false);
    }
}
