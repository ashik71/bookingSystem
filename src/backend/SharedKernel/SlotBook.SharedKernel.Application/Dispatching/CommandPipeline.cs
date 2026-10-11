using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SlotBook.SharedKernel.Application.Persistence;
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
        var execution = services.GetRequiredService<ICommandExecution>();
        return await execution.RunAsync<TResponse>(
            typeof(TCommand),
            services,
            (attemptServices, token) =>
            {
                var handler = attemptServices.GetService<ICommandHandler<TCommand, TResponse>>()
                    ?? throw new InvalidOperationException($"No command handler is registered for {typeof(TCommand)}.");
                return handler.HandleAsync(typed, token);
            },
            cancellationToken).ConfigureAwait(false);
    }
}
