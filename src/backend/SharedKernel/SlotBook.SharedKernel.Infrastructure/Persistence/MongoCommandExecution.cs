using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SlotBook.SharedKernel.Application;
using SlotBook.SharedKernel.Application.Persistence;
using SlotBook.SharedKernel.Domain;

namespace SlotBook.SharedKernel.Infrastructure.Persistence;

#pragma warning disable CA1812 // instantiated by dependency injection

/// <summary>
/// The whole-handler retry loop (AD-7). Each attempt runs in a fresh DI scope, so its unit of work,
/// repositories and loads are new. A version conflict, a write conflict or a retryable duplicate key
/// re-runs the handler after a jittered backoff until the 3 s budget is spent; then the command fails
/// with <c>concurrency_conflict</c> (409). Every other exception passes through unchanged. A refusal
/// the handler returns on a re-run is returned as it is.
/// </summary>
internal sealed class MongoCommandExecution(
    TimeProvider timeProvider,
    RetryableDuplicateKeys retryableDuplicateKeys,
    ILogger<MongoCommandExecution> logger) : ICommandExecution
{
    public async Task<Result<TResponse>> RunAsync<TResponse>(
        Type commandType,
        IServiceProvider services,
        Func<IServiceProvider, CancellationToken, Task<Result<TResponse>>> attempt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(commandType);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(attempt);

        if (AmbientAttempt.Current is { } outer)
        {
            // A nested public command joins the outer attempt: same scope, same unit of work.
            return await attempt(outer.Services, cancellationToken).ConfigureAwait(false);
        }

        var budget = new RetryBudget(timeProvider, RetryBudget.DefaultTotal);
        for (var attempts = 1; ; attempts++)
        {
            Exception conflict;
            var scope = services.GetRequiredService<IServiceScopeFactory>().CreateAsyncScope();
            await using (scope.ConfigureAwait(false))
            {
                AmbientAttempt.Current = new AmbientAttempt(scope.ServiceProvider, budget);
                try
                {
                    return await attempt(scope.ServiceProvider, cancellationToken).ConfigureAwait(false);
                }
                catch (UnknownCommitResultException exception)
                {
                    logger.LogCommitResultUnknown(exception, commandType.Name);
                    throw new DomainException(CommonErrors.ConcurrencyConflict, exception);
                }
                catch (Exception exception) when (WriteErrorClassifier.Classify(exception, retryableDuplicateKeys) == WriteErrorAction.RerunHandler)
                {
                    conflict = exception;
                }
                finally
                {
                    AmbientAttempt.Current = null;
                }
            }

            if (budget.IsExhausted)
            {
                logger.LogRetryExhausted(conflict, commandType.Name, (int)budget.Total.TotalMilliseconds, attempts);
                throw new DomainException(CommonErrors.ConcurrencyConflict, conflict);
            }

            await budget.BackoffAsync(attempts, cancellationToken).ConfigureAwait(false);
        }
    }
}
