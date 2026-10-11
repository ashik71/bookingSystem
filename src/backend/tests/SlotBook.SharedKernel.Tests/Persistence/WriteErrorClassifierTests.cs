using MongoDB.Driver;
using Shouldly;
using SlotBook.SharedKernel.Infrastructure.Persistence;
using Xunit;

namespace SlotBook.SharedKernel.Tests.Persistence;

public class WriteErrorClassifierTests
{
    private const string DuplicateOnId = "E11000 duplicate key error collection: slotbook.counters index: _id_ dup key: { _id: UUID(\"x\") }";
    private const string DuplicateOnRequestKey = "E11000 duplicate key error collection: slotbook.bookings index: ux_bookings_requestKey dup key: { }";

    [Fact]
    public void VersionConflictReRunsTheHandler() =>
        WriteErrorClassifier.Classify(new VersionConflictException("notes", Guid.Empty)).ShouldBe(WriteErrorAction.RerunHandler);

    [Fact]
    public void TransientTransactionErrorReRunsTheHandler() =>
        WriteErrorClassifier.Classify(Labelled("TransientTransactionError")).ShouldBe(WriteErrorAction.RerunHandler);

    [Fact]
    public void UnknownCommitResultRetriesOnlyTheCommit() =>
        WriteErrorClassifier.Classify(Labelled("UnknownTransactionCommitResult")).ShouldBe(WriteErrorAction.RetryCommit);

    [Fact]
    public void AnUnlabelledMongoErrorFails() =>
        WriteErrorClassifier.Classify(new MongoException("boom")).ShouldBe(WriteErrorAction.Fail);

    [Fact]
    public void AnUnrelatedExceptionFails() =>
        WriteErrorClassifier.Classify(new InvalidOperationException("boom")).ShouldBe(WriteErrorAction.Fail);

    [Fact]
    public void DuplicateKeyOnAnyIdIsRetryable() =>
        WriteErrorClassifier.IsRetryableDuplicate(ServerErrorCategory.DuplicateKey, DuplicateOnId, Registry()).ShouldBeTrue();

    [Fact]
    public void DuplicateKeyOnAMarkedIndexIsRetryable() =>
        WriteErrorClassifier.IsRetryableDuplicate(ServerErrorCategory.DuplicateKey, DuplicateOnRequestKey, Registry()).ShouldBeTrue();

    [Fact]
    public void DuplicateKeyOnAnUnmarkedIndexIsNotRetried() =>
        WriteErrorClassifier.IsRetryableDuplicate(ServerErrorCategory.DuplicateKey, DuplicateOnRequestKey, new RetryableDuplicateKeys()).ShouldBeFalse();

    [Fact]
    public void ARetryableIndexNameOnAnotherCollectionIsNotRetried() =>
        WriteErrorClassifier.IsRetryableDuplicate(
            ServerErrorCategory.DuplicateKey,
            DuplicateOnRequestKey.Replace("slotbook.bookings", "slotbook.patients", StringComparison.Ordinal),
            Registry()).ShouldBeFalse();

    [Fact]
    public void ADuplicateKeyMessageWithAnotherCategoryIsNotRetried() =>
        WriteErrorClassifier.IsRetryableDuplicate(ServerErrorCategory.Uncategorized, DuplicateOnId, Registry()).ShouldBeFalse();

    private static MongoException Labelled(string label)
    {
        var exception = new MongoException("boom");
        exception.AddErrorLabel(label);
        return exception;
    }

    private static RetryableDuplicateKeys Registry()
    {
        var keys = new RetryableDuplicateKeys();
        keys.Add("bookings", "ux_bookings_requestKey");
        return keys;
    }
}
