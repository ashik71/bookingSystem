#pragma warning disable CA1515 // public: discovered by MVC and the dispatcher scan
using SlotBook.SharedKernel.Contracts;

namespace SlotBook.IntegrationTests.TestSupport.Guards;

/// <summary>Always conflicts, or conflicts on the first attempt and refuses on the next.</summary>
public sealed record ConflictCommand(string Key, bool RefuseOnRerun) : ICommand<int>;
