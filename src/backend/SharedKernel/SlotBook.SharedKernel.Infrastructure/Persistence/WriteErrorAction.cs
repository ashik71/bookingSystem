namespace SlotBook.SharedKernel.Infrastructure.Persistence;

internal enum WriteErrorAction
{
    /// <summary>Not a conflict: the exception passes through unchanged.</summary>
    Fail,

    /// <summary>Another writer got there first: re-run the whole handler with fresh loads.</summary>
    RerunHandler,

    /// <summary>The commit may have landed: repeat only the commit.</summary>
    RetryCommit,
}
