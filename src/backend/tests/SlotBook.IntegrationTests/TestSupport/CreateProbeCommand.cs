#pragma warning disable CA1515 // public: discovered by MVC and the dispatcher scan
using SlotBook.SharedKernel.Contracts;

namespace SlotBook.IntegrationTests.TestSupport;

public sealed record CreateProbeCommand(string DisplayName, int Count) : ICommand<ProbeCreated>;
