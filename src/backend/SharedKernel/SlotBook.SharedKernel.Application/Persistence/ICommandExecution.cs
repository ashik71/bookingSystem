namespace SlotBook.SharedKernel.Application.Persistence;

/// <summary>
/// The seam the dispatcher runs a validated command through (AD-7). The default runs the attempt
/// once in the caller's scope. Infrastructure replaces it with the retry loop, because this layer
/// cannot see MongoDB (AD-2).
/// </summary>
public interface ICommandExecution
{
    /// <summary>
    /// Runs <paramref name="attempt"/>, possibly several times. Each attempt gets the service provider
    /// of its own scope, from which it must resolve the handler and everything the handler uses.
    /// A nested call (an AD-3 public command sent from a handler) joins the running attempt and is
    /// never retried on its own.
    /// </summary>
    /// <typeparam name="TResponse">The value on success.</typeparam>
    /// <param name="commandType">The command type, for logs.</param>
    /// <param name="services">The caller's service provider, used to open a scope per attempt.</param>
    /// <param name="attempt">One attempt, given the service provider to resolve from.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The result of the attempt that finished.</returns>
    public Task<Result<TResponse>> RunAsync<TResponse>(
        Type commandType,
        IServiceProvider services,
        Func<IServiceProvider, CancellationToken, Task<Result<TResponse>>> attempt,
        CancellationToken cancellationToken);
}
