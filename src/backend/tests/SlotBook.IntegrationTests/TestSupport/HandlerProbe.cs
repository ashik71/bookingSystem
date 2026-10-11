#pragma warning disable CA1515 // public: used by the test-only use cases
namespace SlotBook.IntegrationTests.TestSupport;

public sealed class HandlerProbe
{
    private int _runs;

    public int Runs => _runs;

    public void Ran() => Interlocked.Increment(ref _runs);

    public void Reset() => Interlocked.Exchange(ref _runs, 0);
}
