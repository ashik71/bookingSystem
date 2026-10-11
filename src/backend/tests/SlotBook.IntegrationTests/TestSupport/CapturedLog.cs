#pragma warning disable CA1515 // public: used as an xunit fixture type, or discovered by MVC
namespace SlotBook.IntegrationTests.TestSupport;

public sealed record CapturedLog(string Category, int EventId, string Message, string? TraceId);
