using System.Text.RegularExpressions;
using MongoDB.Driver;

namespace SlotBook.SharedKernel.Infrastructure.Persistence;

/// <summary>Decides what a failed write means (AD-7, AD-8).</summary>
internal static partial class WriteErrorClassifier
{
    internal const string TransientTransactionError = "TransientTransactionError";
    internal const string UnknownTransactionCommitResult = "UnknownTransactionCommitResult";

    internal static WriteErrorAction Classify(Exception exception, RetryableDuplicateKeys? retryable = null)
    {
        ArgumentNullException.ThrowIfNull(exception);

        switch (exception)
        {
            case VersionConflictException:
                return WriteErrorAction.RerunHandler;
            case MongoException mongo when mongo.HasErrorLabel(UnknownTransactionCommitResult):
                return WriteErrorAction.RetryCommit;
            case MongoException mongo when mongo.HasErrorLabel(TransientTransactionError):
                return WriteErrorAction.RerunHandler;
            case MongoWriteException write when IsRetryableDuplicate(write.WriteError, retryable):
                return WriteErrorAction.RerunHandler;
            case MongoBulkWriteException bulk when bulk.WriteErrors.Count > 0 && bulk.WriteErrors.All(error => IsRetryableDuplicate(error, retryable)):
                return WriteErrorAction.RerunHandler;
            default:
                return WriteErrorAction.Fail;
        }
    }

    /// <summary>
    /// A duplicate key counts only when its category is <c>DuplicateKey</c> and the collection and index
    /// named in the error are the expected ones (AD-8): never the exception type alone.
    /// </summary>
    internal static bool IsRetryableDuplicate(ServerErrorCategory category, string message, RetryableDuplicateKeys? retryable)
    {
        if (category != ServerErrorCategory.DuplicateKey || retryable is null)
        {
            return false;
        }

        var match = DuplicateKeyMessage().Match(message);
        if (!match.Success)
        {
            return false;
        }

        var collection = match.Groups["collection"].Value;
        var dot = collection.IndexOf('.', StringComparison.Ordinal);
        collection = dot >= 0 ? collection[(dot + 1)..] : collection;
        return retryable.Contains(collection, match.Groups["index"].Value);
    }

    private static bool IsRetryableDuplicate(WriteError? error, RetryableDuplicateKeys? retryable) =>
        error is not null && IsRetryableDuplicate(error.Category, error.Message, retryable);

    [GeneratedRegex(@"collection: (?<collection>\S+) index: (?<index>\S+)", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex DuplicateKeyMessage();
}
