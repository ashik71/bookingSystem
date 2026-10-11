namespace SlotBook.SharedKernel.Infrastructure.Persistence;

/// <summary>
/// The commit result stayed unknown until the time budget ran out (AD-7). The transaction may have
/// landed, so the handler is not re-run; the command answers <c>concurrency_conflict</c>.
/// </summary>
public sealed class UnknownCommitResultException : Exception
{
    public UnknownCommitResultException(Exception innerException)
        : base("The transaction commit result is unknown.", innerException)
    {
    }

    public UnknownCommitResultException()
        : base("The transaction commit result is unknown.")
    {
    }

    public UnknownCommitResultException(string message)
        : base(message)
    {
    }

    public UnknownCommitResultException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
