using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Shouldly;
using SlotBook.IntegrationTests.TestSupport;
using SlotBook.IntegrationTests.TestSupport.Messaging;
using SlotBook.IntegrationTests.TestSupport.Persistence;
using SlotBook.SharedKernel.Application;
using SlotBook.SharedKernel.Application.Dispatching;
using SlotBook.SharedKernel.Domain;
using SlotBook.SharedKernel.Infrastructure.Persistence;
using Xunit;

namespace SlotBook.IntegrationTests.Messaging;

public sealed class OutboxTests(MongoDatabaseFixture mongo) : IClassFixture<MongoDatabaseFixture>, IAsyncLifetime
{
    private const string TenantSlug = "outbox-tests";
    private static readonly TimeSpan _wait = TimeSpan.FromSeconds(20);
    private static readonly FilterDefinitionBuilder<BsonDocument> _filter = Builders<BsonDocument>.Filter;

    private SlotBookApiFactory? _factory;
    private EventRecorder? _recorder;

    private SlotBookApiFactory Factory => _factory ?? throw new InvalidOperationException("Not initialized.");

    private EventRecorder Recorder => _recorder ?? throw new InvalidOperationException("Not initialized.");

    public async ValueTask InitializeAsync()
    {
        // Collections must exist before they are touched inside a transaction.
        var ct = TestContext.Current.CancellationToken;
        var existing = await (await mongo.Database.ListCollectionNamesAsync(cancellationToken: ct)).ToListAsync(ct);
        if (!existing.Contains(NoteWriteRepository.CollectionName, StringComparer.Ordinal))
        {
            await mongo.Database.CreateCollectionAsync(NoteWriteRepository.CollectionName, cancellationToken: ct);
        }

        _factory = new SlotBookApiFactory { TenantSlug = TenantSlug, MongoDatabase = mongo.DatabaseName };
        _recorder = await EventRecorder.StartAsync(Factory.Services.GetRequiredService<IBus>(), Factory.EndpointPrefix + "-recorder", [typeof(NoteCreated)], evt => evt.TenantId == DeterministicId.ForTenant(TenantSlug), ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (_recorder is not null)
        {
            await _recorder.DisposeAsync();
        }

        if (_factory is not null)
        {
            await OutboxDrain.WaitAsync(mongo.Database, _wait, TestContext.Current.CancellationToken);
            await _factory.DisposeAsync();
        }
    }

    [Fact]
    public async Task AggregateAndEventCommitTogetherAndTheEventIsDelivered()
    {
        var ct = TestContext.Current.CancellationToken;
        var noteId = Id("commit");
        var before = Recorder.All.Count;

        var result = await SendAsync(new CreateNoteWithEventCommand(noteId, "hello", CreateNoteMode.Commit), ct);

        result.IsRefused.ShouldBeFalse();
        (await NotesCountAsync(noteId, ct)).ShouldBe(1);
        var received = (await Recorder.WaitForAsync(before + 1, _wait, ct)).Where(item => item.Message is NoteCreated { NoteId: var id } && id == noteId).ToList();
        var created = received.Single().Message.ShouldBeOfType<NoteCreated>();
        created.NoteId.ShouldBe(noteId);
        created.Text.ShouldBe("hello");
        created.TenantId.ShouldBe(DeterministicId.ForTenant(TenantSlug));
        received.Single().MessageId.ShouldBe(created.EventId, "the event id is the message id on the bus");
    }

    [Fact]
    public async Task AggregateAndOutboxMessageAreInOneTransaction()
    {
        var ct = TestContext.Current.CancellationToken;
        var noteId = Id("one-transaction");
        var before = Recorder.All.Count;
        using var scope = Factory.Services.CreateScope();
        var notes = scope.ServiceProvider.GetRequiredService<NoteWriteRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        notes.Add(new Note(noteId, "atomic"));
        unitOfWork.AddIntegrationEvent(NewEvent(scope, noteId, "atomic"));

        await unitOfWork.CommitAsync(ct);

        // Same transaction means the note and the outbox row share one commit: both are visible now.
        (await NotesCountAsync(noteId, ct)).ShouldBe(1);
        (await Recorder.WaitForAsync(before + 1, _wait, ct)).Count.ShouldBe(before + 1);
    }

    [Fact]
    public async Task HandlerThatThrowsAfterStagingPublishesNothing()
    {
        var ct = TestContext.Current.CancellationToken;
        var noteId = Id("throws");
        var before = Recorder.All.Count;

        await Should.ThrowAsync<InvalidOperationException>(() => SendAsync(new CreateNoteWithEventCommand(noteId, "never", CreateNoteMode.ThrowAfterStaging), ct));

        (await NotesCountAsync(noteId, ct)).ShouldBe(0);
        await OutboxDrain.WaitAsync(mongo.Database, _wait, ct);
        await Task.Delay(TimeSpan.FromSeconds(2), ct);
        (await OutboxDrain.CountAsync(mongo.Database, ct)).ShouldBe(0);
        Recorder.All.Count.ShouldBe(before, "nothing reached the broker");
    }

    [Fact]
    public async Task ReRunAfterConflictPublishesOnce()
    {
        var ct = TestContext.Current.CancellationToken;
        var existing = Id("conflict-existing");
        var noteId = Id("conflict");
        using (var scope = Factory.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<NoteWriteRepository>().Add(new Note(existing, "existing"));
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync(ct);
        }

        var before = Recorder.All.Count;

        var result = await SendAsync(new CreateNoteWithEventCommand(noteId, "rerun", CreateNoteMode.ConflictOnce, existing), ct);

        result.IsRefused.ShouldBeFalse();
        Factory.Services.GetRequiredService<TestSupport.Guards.ConflictAttempts>().Next(noteId.ToString()).ShouldBe(3, "the handler ran twice: the conflict, then the re-run");
        (await NotesCountAsync(noteId, ct)).ShouldBe(1);
        await Recorder.WaitForAsync(before + 1, _wait, ct);
        await OutboxDrain.WaitAsync(mongo.Database, _wait, ct);
        await Task.Delay(TimeSpan.FromSeconds(1), ct);
        Recorder.All.Count.ShouldBe(before + 1, "exactly one event was published");
        Recorder.All.Count(received => received.Message is NoteCreated created && created.NoteId == noteId).ShouldBe(1);
    }

    [Fact]
    public async Task NothingIsDeliveredBeforeTheCommit()
    {
        var ct = TestContext.Current.CancellationToken;
        var noteId = Id("before-commit");
        var before = Recorder.All.Count;
        using var scope = Factory.Services.CreateScope();
        var notes = scope.ServiceProvider.GetRequiredService<NoteWriteRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        notes.Add(new Note(noteId, "held"));
        unitOfWork.AddIntegrationEvent(NewEvent(scope, noteId, "held"));

        await Task.Delay(TimeSpan.FromSeconds(2), ct);
        Recorder.All.Count.ShouldBe(before, "a staged event stays in memory until the commit");
        (await OutboxDrain.CountAsync(mongo.Database, ct)).ShouldBe(0);

        await unitOfWork.CommitAsync(ct);

        (await Recorder.WaitForAsync(before + 1, _wait, ct)).Count.ShouldBe(before + 1);
    }

    [Fact]
    public async Task OutboxMessageIsWrittenOnTheUnitOfWorksTransaction()
    {
        var ct = TestContext.Current.CancellationToken;
        var before = Recorder.All.Count;

        // Abandoned transaction: the outbox row is invisible from outside, and abort removes it.
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var unitOfWork = scope.ServiceProvider.GetRequiredService<MongoUnitOfWork>();
            await unitOfWork.BeginAsync(ct);
            await scope.ServiceProvider.GetRequiredService<IPublishEndpoint>().Publish(NewEvent(scope, Id("session-abandoned"), "abandoned"), ct);

            (await OutboxDrain.CountAsync(mongo.Database, ct)).ShouldBe(0, "the outbox row is part of the open transaction");
        }

        await Task.Delay(TimeSpan.FromSeconds(2), ct);
        (await OutboxDrain.CountAsync(mongo.Database, ct)).ShouldBe(0);
        Recorder.All.Count.ShouldBe(before, "an aborted transaction delivers nothing");

        // The same publish, committed with the unit of work's own session, is delivered.
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            var unitOfWork = scope.ServiceProvider.GetRequiredService<MongoUnitOfWork>();
            var session = await unitOfWork.BeginAsync(ct);
            await scope.ServiceProvider.GetRequiredService<IPublishEndpoint>().Publish(NewEvent(scope, Id("session-committed"), "committed"), ct);
            await session.CommitTransactionAsync(ct);
        }

        (await Recorder.WaitForAsync(before + 1, _wait, ct)).Count.ShouldBe(before + 1);
    }

    private static NoteCreated NewEvent(IServiceScope scope, Guid noteId, string text)
    {
        var stamp = EventStamp.Now(scope.ServiceProvider.GetRequiredService<IClock>());
        return new NoteCreated
        {
            EventId = stamp.EventId,
            TenantId = scope.ServiceProvider.GetRequiredService<ITenantContext>().TenantId,
            OccurredAt = stamp.OccurredAt,
            NoteId = noteId,
            Text = text,
        };
    }

    private static Guid Id(string name) => DeterministicId.From(DeterministicId.SlotBookNamespace, "outbox-test:" + name);

    private Task<Result<Guid>> SendAsync(CreateNoteWithEventCommand command, CancellationToken ct) =>
        Factory.Services.GetRequiredService<IDispatcher>().SendAsync(command, ct);

    private async Task<long> NotesCountAsync(Guid id, CancellationToken ct) =>
        await mongo.Database.GetCollection<BsonDocument>(NoteWriteRepository.CollectionName)
            .CountDocumentsAsync(_filter.Eq("_id", new BsonBinaryData(id, GuidRepresentation.Standard)), cancellationToken: ct);
}
