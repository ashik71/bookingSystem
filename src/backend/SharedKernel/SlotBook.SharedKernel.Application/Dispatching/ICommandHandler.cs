using SlotBook.SharedKernel.Contracts;

namespace SlotBook.SharedKernel.Application.Dispatching;

/// <summary>Handles one command type. Runs only after every validator passed.</summary>
/// <typeparam name="TCommand">The command.</typeparam>
/// <typeparam name="TResponse">The value on success.</typeparam>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    /// <summary>Handles the command.</summary>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The value, or a refusal.</returns>
    public Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
