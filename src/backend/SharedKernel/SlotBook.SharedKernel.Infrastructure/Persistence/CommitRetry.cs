using MongoDB.Driver;

namespace SlotBook.SharedKernel.Infrastructure.Persistence;

/// <summary>
/// The commit-only retry (AD-7): an <c>UnknownTransactionCommitResult</c> means the commit may have
/// landed, so only the commit is repeated, within the command's budget. The handler never re-runs.
/// </summary>
internal static class CommitRetry
{
    internal static async Task RunAsync(Func<Task> commit, RetryBudget? budget, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(commit);

        for (var retry = 1; ; retry++)
        {
            try
            {
                await commit().ConfigureAwait(false);
                return;
            }
            catch (MongoException exception) when (budget is not null && WriteErrorClassifier.Classify(exception) == WriteErrorAction.RetryCommit)
            {
                if (budget.IsExhausted)
                {
                    throw new UnknownCommitResultException(exception);
                }

                await budget.BackoffAsync(retry, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
