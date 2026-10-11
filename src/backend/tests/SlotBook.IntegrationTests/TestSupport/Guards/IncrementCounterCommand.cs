#pragma warning disable CA1515 // public: discovered by MVC and the dispatcher scan
using SlotBook.SharedKernel.Contracts;

namespace SlotBook.IntegrationTests.TestSupport.Guards;

public sealed record IncrementCounterCommand(string Key) : ICommand<int>;
