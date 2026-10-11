using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SlotBook.SharedKernel.Contracts;

namespace SlotBook.SharedKernel.Application.Dispatching;

#pragma warning disable CA1812 // instantiated by dependency injection

internal sealed class CommandPipeline<TCommand, TResponse>(IServiceProvider services)
    : Dispatcher.CommandPipelineBase<TResponse>
    where TCommand : ICommand<TResponse>
{
    public override async Task<Result<TResponse>> RunAsync(ICommand<TResponse> command, CancellationToken cancellationToken)
    {
        var typed = (TCommand)command;
        await Dispatcher.ValidateAsync(services.GetServices<IValidator<TCommand>>(), typed, cancellationToken).ConfigureAwait(false);
        var handler = services.GetService<ICommandHandler<TCommand, TResponse>>()
            ?? throw new InvalidOperationException($"No command handler is registered for {typeof(TCommand)}.");
        return await handler.HandleAsync(typed, cancellationToken).ConfigureAwait(false);
    }
}
