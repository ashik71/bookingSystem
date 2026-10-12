#pragma warning disable CA1515 // fixture
using SlotBook.SharedKernel.Contracts;

namespace SlotBook.Modules.Fixture.Messaging;

/// <summary>Violation: not sealed.</summary>
public record FixtureUnsealed : TenantEvent;
