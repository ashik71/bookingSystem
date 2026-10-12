#pragma warning disable CA1515 // public: used by the test classes
namespace SlotBook.IntegrationTests.TestSupport.Jobs;

/// <summary>One finished run: the lease holder that ran it, and the start and end as stopwatch timestamps.</summary>
public sealed record RecordedRun(string Holder, long Start, long End);
