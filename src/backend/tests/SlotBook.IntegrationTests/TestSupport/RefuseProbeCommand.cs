#pragma warning disable CA1515 // public: discovered by MVC and the dispatcher scan
using SlotBook.SharedKernel.Contracts;

namespace SlotBook.IntegrationTests.TestSupport;

/// <summary>Handler refuses with <see cref="Code"/>.</summary>
/// <param name="Code">The refusal code.</param>
public sealed record RefuseProbeCommand(string Code) : ICommand<ProbeCreated>;
