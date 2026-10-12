#pragma warning disable CA1515 // public: part of a public command
namespace SlotBook.IntegrationTests.TestSupport.Messaging;

public enum ConsumerBehavior
{
    /// <summary>Write the counter and commit.</summary>
    Normal,

    /// <summary>Write the counter, stage a <c>NoteArchived</c> follow-up event, commit.</summary>
    StageFollowUp,

    /// <summary>Throw a <c>DomainException</c> before writing.</summary>
    DomainFailure,

    /// <summary>On the first attempt throw a transient error before writing.</summary>
    TransientFailureOnce,

    /// <summary>On the first attempt throw a version conflict before writing.</summary>
    ConflictOnce,

    /// <summary>Return a refusal after writing the counter.</summary>
    Refuse,
}
