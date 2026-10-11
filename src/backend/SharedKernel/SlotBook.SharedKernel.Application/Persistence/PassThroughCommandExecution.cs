namespace SlotBook.SharedKernel.Application.Persistence;

#pragma warning disable CA1812 // instantiated by dependency injection

/// <summary>Runs the attempt once in the caller's scope: no retry.</summary>
internal sealed class PassThroughCommandExecution : ICommandExecution
{
    public Task<Result<TResponse>> RunAsync<TResponse>(
        Type commandType,
        IServiceProvider services,
        Func<IServiceProvider, CancellationToken, Task<Result<TResponse>>> attempt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(attempt);
        return attempt(services, cancellationToken);
    }
}
