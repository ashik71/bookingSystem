using SlotBook.SharedKernel.Contracts;

namespace SlotBook.SharedKernel.Application.Dispatching;

/// <summary>Handles one query type. Runs only after every validator passed.</summary>
/// <typeparam name="TQuery">The query.</typeparam>
/// <typeparam name="TResponse">The value returned.</typeparam>
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    /// <summary>Handles the query.</summary>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">Cancellation.</param>
    /// <returns>The value.</returns>
    public Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
