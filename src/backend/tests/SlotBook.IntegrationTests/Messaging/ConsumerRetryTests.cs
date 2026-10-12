using System.Diagnostics;
using Shouldly;
using SlotBook.IntegrationTests.TestSupport;
using SlotBook.IntegrationTests.TestSupport.Messaging;
using Xunit;

namespace SlotBook.IntegrationTests.Messaging;

public sealed class ConsumerRetryTests(MongoDatabaseFixture mongo, RabbitMqFixture rabbit) : IClassFixture<MongoDatabaseFixture>, IClassFixture<RabbitMqFixture>, IAsyncLifetime
{
    private ConsumerFixture? _consumers;

    private ConsumerFixture Consumers => _consumers ?? throw new InvalidOperationException("Not initialized.");

    public async ValueTask InitializeAsync()
    {
        _consumers = new ConsumerFixture(mongo, "retry-tests");
        await Consumers.StartAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_consumers is not null)
        {
            await _consumers.DisposeAsync();
        }
    }

    [Fact]
    public async Task DomainExceptionGoesToErrorQueueWithoutRetry()
    {
        var ct = TestContext.Current.CancellationToken;
        var noteId = ConsumerFixture.Id("DomainExceptionGoesToErrorQueueWithoutRetry", "1");
        var messageId = ConsumerFixture.Id("DomainExceptionGoesToErrorQueueWithoutRetry", "2");
        Consumers.Host.Probe.Expect(noteId, ConsumerBehavior.DomainFailure);

        await Consumers.PublishAsync(noteId, messageId, ct);

        var found = new List<string>();
        await ConsumerFixture.WaitUntilAsync(
            async () =>
            {
                found.AddRange(await Consumers.ErrorQueueMessageIdsAsync(rabbit.Connection, ct));
                return found.Contains(messageId.ToString(), StringComparer.OrdinalIgnoreCase);
            },
            "the message in the error queue",
            ct);
        await Task.Delay(TimeSpan.FromSeconds(2), ct);

        Consumers.Host.Probe.Invocations(noteId).ShouldBe(1, "a deterministic failure is not retried");
        (await Consumers.CounterAsync(noteId, ct)).ShouldBe(0);
    }

    [Fact]
    public async Task TransientFailureIsRetried()
    {
        var ct = TestContext.Current.CancellationToken;
        var noteId = ConsumerFixture.Id("TransientFailureIsRetried", "1");
        var messageId = ConsumerFixture.Id("TransientFailureIsRetried", "2");
        Consumers.Host.Probe.Expect(noteId, ConsumerBehavior.TransientFailureOnce);

        await Consumers.PublishAsync(noteId, messageId, ct);
        await ConsumerFixture.WaitUntilAsync(async () => await Consumers.CounterAsync(noteId, ct) > 0, "the retried consume", ct);
        await Task.Delay(TimeSpan.FromSeconds(2), ct);

        (await Consumers.CounterAsync(noteId, ct)).ShouldBe(1);
        Consumers.Host.Probe.Invocations(noteId).ShouldBe(2, "failed once, then succeeded after the 1 s interval");
        (await Consumers.ErrorQueueMessageIdsAsync(rabbit.Connection, ct)).ShouldNotContain(messageId.ToString());
    }

    [Fact]
    public async Task WriteConflictIsRetriedBeforeTheIntervalPolicy()
    {
        var ct = TestContext.Current.CancellationToken;
        var noteId = ConsumerFixture.Id("WriteConflictIsRetriedBeforeTheIntervalPolicy", "1");
        Consumers.Host.Probe.Expect(noteId, ConsumerBehavior.ConflictOnce);

        var clock = Stopwatch.StartNew();
        await Consumers.PublishAsync(noteId, ConsumerFixture.Id("WriteConflictIsRetriedBeforeTheIntervalPolicy", "2"), ct);
        await ConsumerFixture.WaitUntilAsync(async () => await Consumers.CounterAsync(noteId, ct) > 0, "the re-run consume", ct);
        clock.Stop();

        (await Consumers.CounterAsync(noteId, ct)).ShouldBe(1);
        Consumers.Host.Probe.Attempts(noteId).ShouldBe(2);
        clock.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(1), "the 1 s interval retry was not needed");
    }

    [Fact]
    public async Task RefusalCommitsTheInboxMarkAndIsNotRetried()
    {
        var ct = TestContext.Current.CancellationToken;
        var noteId = ConsumerFixture.Id("RefusalCommitsTheInboxMarkAndIsNotRetried", "1");
        var messageId = ConsumerFixture.Id("RefusalCommitsTheInboxMarkAndIsNotRetried", "2");
        Consumers.Host.Probe.Expect(noteId, ConsumerBehavior.Refuse);

        await Consumers.PublishAsync(noteId, messageId, ct);
        await ConsumerFixture.WaitUntilAsync(async () => await Consumers.InboxRecordsAsync(messageId, ct) > 0, "the inbox mark", ct);
        await Task.Delay(TimeSpan.FromSeconds(2), ct);

        Consumers.Host.Probe.Invocations(noteId).ShouldBe(1);
        (await Consumers.ErrorQueueMessageIdsAsync(rabbit.Connection, ct)).ShouldNotContain(messageId.ToString());
        Consumers.Host.Logs.Records.ShouldContain(log => log.EventId == 11 && log.Message.Contains("note_refused", StringComparison.Ordinal));
    }
}
