#pragma warning disable CA1515 // public: serialized by MVC
namespace SlotBook.IntegrationTests.TestSupport;

public sealed record ProbeCreated(string DisplayName, int Count);
