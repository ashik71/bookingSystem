using SlotBook.SharedKernel.Contracts;

namespace SlotBook.SharedKernel.Application.Dispatching;

/// <summary>The only thing a controller depends on (AD-4).</summary>
public interface IDispatcher
{
    /// <summary>Validates, then handles a command.</summary>
    /// <typeparam name="TResponse">The value on success.</typeparam>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The value, or a refusal.</returns>
    /// <exception cref="Validation.ValidationFailedException">A validator failed; the handler did not run.</exception>
    public Task<Result<TResponse>> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);

    /// <summary>Validates, then handles a query.</summary>
    /// <typeparam name="TResponse">The value returned.</typeparam>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The value.</returns>
    /// <exception cref="Validation.ValidationFailedException">A validator failed; the handler did not run.</exception>
    public Task<TResponse> QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);
}
