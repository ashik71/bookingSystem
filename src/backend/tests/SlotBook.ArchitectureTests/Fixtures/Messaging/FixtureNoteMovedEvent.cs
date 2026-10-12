#pragma warning disable CA1515 // fixture
using SlotBook.SharedKernel.Contracts;

namespace SlotBook.Modules.Fixture.Messaging;

/// <summary>Violation: an <c>Event</c> suffix.</summary>
public sealed record FixtureNoteMovedEvent : TenantEvent;
