using System.Collections.Concurrent;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SlotBook.SharedKernel.Application.Validation;
using SlotBook.SharedKernel.Contracts;

namespace SlotBook.SharedKernel.Application.Dispatching;

#pragma warning disable CA1812 // instantiated by dependency injection

internal sealed class Dispatcher(IServiceProvider services) : IDispatcher
{
    private static readonly ConcurrentDictionary<Type, Type> _commandPipelines = new();
    private static readonly ConcurrentDictionary<Type, Type> _queryPipelines = new();

    public Task<Result<TResponse>> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var type = _commandPipelines.GetOrAdd(
            command.GetType(),
            t => typeof(CommandPipeline<,>).MakeGenericType(t, typeof(TResponse)));
        var pipeline = (CommandPipelineBase<TResponse>)services.GetRequiredService(type);
        return pipeline.RunAsync(command, cancellationToken);
    }

    public Task<TResponse> QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var type = _queryPipelines.GetOrAdd(
            query.GetType(),
            t => typeof(QueryPipeline<,>).MakeGenericType(t, typeof(TResponse)));
        var pipeline = (QueryPipelineBase<TResponse>)services.GetRequiredService(type);
        return pipeline.RunAsync(query, cancellationToken);
    }

    internal static async Task ValidateAsync<TRequest>(
        IEnumerable<IValidator<TRequest>> validators,
        TRequest request,
        CancellationToken cancellationToken)
    {
        var failures = new List<FluentValidation.Results.ValidationFailure>();
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
            failures.AddRange(result.Errors);
        }

        if (failures.Count > 0)
        {
            throw new ValidationFailedException(ValidationCodes.ToErrors(failures));
        }
    }

    internal abstract class CommandPipelineBase<TResponse>
    {
        public abstract Task<Result<TResponse>> RunAsync(ICommand<TResponse> command, CancellationToken cancellationToken);
    }

    internal abstract class QueryPipelineBase<TResponse>
    {
        public abstract Task<TResponse> RunAsync(IQuery<TResponse> query, CancellationToken cancellationToken);
    }
}
