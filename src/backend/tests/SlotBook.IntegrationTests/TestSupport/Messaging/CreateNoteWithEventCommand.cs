#pragma warning disable CA1515 // public: discovered by the dispatcher scan
using SlotBook.SharedKernel.Contracts;

namespace SlotBook.IntegrationTests.TestSupport.Messaging;

/// <summary>Adds a note and stages <see cref="NoteCreated"/>. <see cref="Mode"/> picks how the handler ends.</summary>
public sealed record CreateNoteWithEventCommand(Guid NoteId, string Text, CreateNoteMode Mode, Guid ExistingNoteId = default) : ICommand<Guid>;
