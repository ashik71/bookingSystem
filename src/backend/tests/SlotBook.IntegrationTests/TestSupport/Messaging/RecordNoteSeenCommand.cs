#pragma warning disable CA1515 // public: discovered by the dispatcher scan
using SlotBook.SharedKernel.Contracts;

namespace SlotBook.IntegrationTests.TestSupport.Messaging;

/// <summary>The consumer's command: increments the counter of the note. <see cref="Behavior"/> picks how the handler ends.</summary>
public sealed record RecordNoteSeenCommand(Guid NoteId, ConsumerBehavior Behavior) : ICommand<int>;
