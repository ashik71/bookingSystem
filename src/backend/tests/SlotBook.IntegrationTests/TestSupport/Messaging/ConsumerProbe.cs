#pragma warning disable CA1515 // public: injected into a public consumer
using System.Collections.Concurrent;

namespace SlotBook.IntegrationTests.TestSupport.Messaging;

/// <summary>Singleton of a test host: what each consumer invocation and handler attempt did, and what the next one should do.</summary>
public sealed class ConsumerProbe
{
    private readonly ConcurrentDictionary<Guid, ConsumerBehavior> _behaviors = new();
    private readonly ConcurrentDictionary<Guid, int> _invocations = new();
    private readonly ConcurrentDictionary<Guid, int> _attempts = new();
    private readonly ConcurrentDictionary<Guid, int> _faults = new();

    public void Expect(Guid noteId, ConsumerBehavior behavior) => _behaviors[noteId] = behavior;

    public ConsumerBehavior BehaviorFor(Guid noteId) => _behaviors.GetValueOrDefault(noteId, ConsumerBehavior.Normal);

    /// <summary>Makes <see cref="FaultInjection"/> throw once after the consumer of this note returned.</summary>
    public void FailOnceAfterHandler(Guid noteId) => _faults[noteId] = 1;

    public bool TakeFault(Guid noteId) => _faults.TryUpdate(noteId, 0, 1);

    public int NextInvocation(Guid noteId) => _invocations.AddOrUpdate(noteId, 1, (_, count) => count + 1);

    public int Invocations(Guid noteId) => _invocations.GetValueOrDefault(noteId);

    public int NextAttempt(Guid noteId) => _attempts.AddOrUpdate(noteId, 1, (_, count) => count + 1);

    public int Attempts(Guid noteId) => _attempts.GetValueOrDefault(noteId);
}
