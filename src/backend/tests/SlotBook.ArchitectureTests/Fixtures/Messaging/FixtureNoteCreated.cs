#pragma warning disable CA1515 // fixture
using SlotBook.SharedKernel.Contracts;

namespace SlotBook.Modules.Fixture.Messaging;

/// <summary>Not a violation: sealed, past tense, no suffix.</summary>
public sealed record FixtureNoteCreated : TenantEvent;
