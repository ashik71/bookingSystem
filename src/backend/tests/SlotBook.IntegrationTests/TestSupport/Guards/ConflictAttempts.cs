#pragma warning disable CA1812 // resolved from the container
using System.Collections.Concurrent;

namespace SlotBook.IntegrationTests.TestSupport.Guards;

/// <summary>Counts attempts per key across the fresh scopes of a retry loop.</summary>
internal sealed class ConflictAttempts
{
    private readonly ConcurrentDictionary<string, int> _counts = new(StringComparer.Ordinal);

    public int Next(string key) => _counts.AddOrUpdate(key, 1, (_, count) => count + 1);
}
