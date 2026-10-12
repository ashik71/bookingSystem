using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SlotBook.IntegrationTests.TestSupport;
using SlotBook.IntegrationTests.TestSupport.Messaging;
using SlotBook.SharedKernel.Application.Dispatching;
using SlotBook.SharedKernel.Domain;
using Xunit;

namespace SlotBook.IntegrationTests.Messaging;

public sealed class InboxTests(MongoDatabaseFixture mongo) : IClassFixture<MongoDatabaseFixture>, IAsyncLifetime
{
    private ConsumerFixture? _consumers;

    private ConsumerFixture Consumers => _consumers ?? throw new InvalidOperationException("Not initialized.");

    public async ValueTask InitializeAsync()
    {
        _consumers = new ConsumerFixture(mongo, "inbox-tests");
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
    public async Task ConsumerRunsOnceAndWritesInTheConsumeTransaction()
    {
        var ct = TestContext.Current.CancellationToken;
        var noteId = ConsumerFixture.Id("ConsumerRunsOnceAndWritesInTheConsumeTransaction", "1");

        // The aggregate and the event commit in one transaction; the bus delivers the event to the consumer.
        using (var scope = Consumers.Host.Host.Services.CreateScope())
        {
            var created = await scope.ServiceProvider.GetRequiredService<IDispatcher>().SendAsync(new CreateNoteWithEventCommand(noteId, "once", CreateNoteMode.Commit), ct);
            created.IsRefused.ShouldBeFalse();
        }

        await ConsumerFixture.WaitUntilAsync(async () => await Consumers.CounterAsync(noteId, ct) > 0, "the consumer's write", ct);
        await Task.Delay(TimeSpan.FromSeconds(2), ct);

        (await Consumers.CounterAsync(noteId, ct)).ShouldBe(1);
        Consumers.Host.Probe.Invocations(noteId).ShouldBe(1);
    }

    [Fact]
    public async Task RedeliveryOfTheSameMessageIsDropped()
    {
        var ct = TestContext.Current.CancellationToken;
        var noteId = ConsumerFixture.Id("RedeliveryOfTheSameMessageIsDropped", "1");
        var messageId = ConsumerFixture.Id("RedeliveryOfTheSameMessageIsDropped", "2");
        await Consumers.PublishAsync(noteId, messageId, ct);
        await ConsumerFixture.WaitUntilAsync(async () => await Consumers.CounterAsync(noteId, ct) > 0, "the first consume", ct);

        // What the broker does with an unacked message after a crash: it delivers it again under the same id.
        await Consumers.PublishAsync(noteId, messageId, ct);
        await Task.Delay(TimeSpan.FromSeconds(3), ct);

        (await Consumers.CounterAsync(noteId, ct)).ShouldBe(1);
        Consumers.Host.Probe.Invocations(noteId).ShouldBe(1);
        (await Consumers.InboxRecordsAsync(messageId, ct)).ShouldBe(1);
        Consumers.Host.Logs.Records.ShouldContain(log => log.EventId == 20 && log.Message.Contains(messageId.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task FailureAfterHandlerRollsBackItsWrites()
    {
        var ct = TestContext.Current.CancellationToken;
        var noteId = ConsumerFixture.Id("FailureAfterHandlerRollsBackItsWrites", "1");
        var messageId = ConsumerFixture.Id("FailureAfterHandlerRollsBackItsWrites", "2");
        Consumers.Host.Probe.FailOnceAfterHandler(noteId);

        await Consumers.PublishAsync(noteId, messageId, ct);

        // The first attempt wrote and then failed before the inbox commit; the retry (after 1 s) runs it again.
        await ConsumerFixture.WaitUntilAsync(async () => await Consumers.CounterAsync(noteId, ct) > 0, "the retried consume", ct);
        await Task.Delay(TimeSpan.FromSeconds(2), ct);

        (await Consumers.CounterAsync(noteId, ct)).ShouldBe(1, "the first attempt's write was rolled back");
        Consumers.Host.Probe.Invocations(noteId).ShouldBe(2);
        Consumers.Host.Probe.Attempts(noteId).ShouldBe(2);
        (await Consumers.InboxRecordsAsync(messageId, ct)).ShouldBe(1);
    }

    [Fact]
    public async Task EventStagedInConsumerCommitsWithInboxMark()
    {
        var ct = TestContext.Current.CancellationToken;
        var noteId = ConsumerFixture.Id("EventStagedInConsumerCommitsWithInboxMark", "1");
        Consumers.Host.Probe.Expect(noteId, ConsumerBehavior.StageFollowUp);
        await using var recorder = await EventRecorder.StartAsync(
            Consumers.Host.Host.Services.GetRequiredService<IBus>(),
            Consumers.Host.EndpointPrefix + "-follow-up",
            [typeof(NoteArchived)],
            evt => evt is NoteArchived archived && archived.NoteId == noteId,
            ct);

        await Consumers.PublishAsync(noteId, ConsumerFixture.Id("EventStagedInConsumerCommitsWithInboxMark", "2"), ct);

        await recorder.WaitForAsync(1, ConsumerFixture.Wait, ct);
        await Task.Delay(TimeSpan.FromSeconds(2), ct);
        recorder.All.Count.ShouldBe(1, "the follow-up event is delivered once");
        (await Consumers.CounterAsync(noteId, ct)).ShouldBe(1);
    }
}
