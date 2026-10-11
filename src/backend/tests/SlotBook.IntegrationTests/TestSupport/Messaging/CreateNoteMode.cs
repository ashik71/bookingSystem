#pragma warning disable CA1515 // public: part of a public command
namespace SlotBook.IntegrationTests.TestSupport.Messaging;

public enum CreateNoteMode
{
    /// <summary>Stage the event and commit.</summary>
    Commit,

    /// <summary>Stage the event, then throw before committing.</summary>
    ThrowAfterStaging,

    /// <summary>On the first attempt, change <c>ExistingNoteId</c> behind the handler's back so the commit conflicts; the re-run commits.</summary>
    ConflictOnce,
}
