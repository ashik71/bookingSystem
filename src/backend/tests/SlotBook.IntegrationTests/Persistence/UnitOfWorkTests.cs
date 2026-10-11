using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Shouldly;
using SlotBook.IntegrationTests.TestSupport;
using SlotBook.IntegrationTests.TestSupport.Persistence;
using SlotBook.SharedKernel.Application;
using SlotBook.SharedKernel.Domain;
using SlotBook.SharedKernel.Infrastructure.Persistence;
using Xunit;

namespace SlotBook.IntegrationTests.Persistence;

public sealed class UnitOfWorkTests(MongoDatabaseFixture mongo) : IClassFixture<MongoDatabaseFixture>, IAsyncLifetime
{
    private static readonly FilterDefinitionBuilder<BsonDocument> _filter = Builders<BsonDocument>.Filter;

    private readonly List<SlotBookApiFactory> _factories = [];

    public async ValueTask InitializeAsync()
    {
        // Collections must exist before they are touched inside a transaction.
        var ct = TestContext.Current.CancellationToken;
        var existing = await (await mongo.Database.ListCollectionNamesAsync(cancellationToken: ct)).ToListAsync(ct);
        foreach (var name in new[] { NoteWriteRepository.CollectionName, GlobalNote.CollectionName }.Except(existing))
        {
            await mongo.Database.CreateCollectionAsync(name, cancellationToken: ct);
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var factory in _factories)
        {
            await factory.DisposeAsync();
        }
    }

    [Fact]
    public async Task TwoWritesCommitTogetherAndStampTheDocument()
    {
        var ct = TestContext.Current.CancellationToken;
        var host = Host("clinic-a");
        var first = Id("two-writes-1");
        var second = Id("two-writes-2");

        using (var scope = host.Services.CreateScope())
        {
            var notes = scope.ServiceProvider.GetRequiredService<NoteWriteRepository>();
            notes.Add(new Note(first, "one"));
            notes.Add(new Note(second, "two"));
            (await RawNotes().CountDocumentsAsync(_filter.Eq("_id", Bin(first)), cancellationToken: ct)).ShouldBe(0, "nothing is written before the commit");

            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync(ct);
        }

        foreach (var id in new[] { first, second })
        {
            var document = await RawNoteAsync(id);
            document["tenantId"].AsGuid.ShouldBe(TenantOf("clinic-a"));
            document["version"].AsInt64.ShouldBe(1);
            document["createdAt"].ToUniversalTime().ShouldBe(document["updatedAt"].ToUniversalTime());
            document["kind"].AsString.ShouldBe("Plain", "enums are stored as strings");
            document.Names.ShouldContain("text", "element names are camelCase");
            document["_id"].AsBsonBinaryData.SubType.ShouldBe(BsonBinarySubType.UuidStandard);
        }
    }

    [Fact]
    public async Task DisposingWithoutCommitWritesNothing()
    {
        var ct = TestContext.Current.CancellationToken;
        var host = Host("clinic-a");
        var first = Id("abandoned-1");
        var second = Id("abandoned-2");

        using (var scope = host.Services.CreateScope())
        {
            var notes = scope.ServiceProvider.GetRequiredService<NoteWriteRepository>();
            notes.Add(new Note(first, "one"));
            notes.Add(new Note(second, "two"));
            _ = await notes.GetAsync(Id("abandoned-nothing"), ct);
        }

        (await RawNotes().CountDocumentsAsync(_filter.In("_id", new[] { Bin(first), Bin(second) }), cancellationToken: ct)).ShouldBe(0);
    }

    [Fact]
    public async Task AHandlerThatThrowsAfterALoadLeavesNoOpenTransaction()
    {
        var ct = TestContext.Current.CancellationToken;
        var host = Host("clinic-a");
        var id = Id("throws-after-load");
        await CommitAsync(host, notes => notes.Add(new Note(id, "original")));

        await using (var scope = host.Services.CreateAsyncScope())
        {
            (await scope.ServiceProvider.GetRequiredService<NoteWriteRepository>().GetAsync(id, ct)).ShouldNotBeNull();
            scope.ServiceProvider.GetRequiredService<MongoUnitOfWork>().Session!.IsInTransaction.ShouldBeTrue();
        }

        // A transaction left open would hold the document and make this write fail with WriteConflict.
        await using (var scope = host.Services.CreateAsyncScope())
        {
            var loaded = await scope.ServiceProvider.GetRequiredService<NoteWriteRepository>().GetAsync(id, ct);
            loaded!.Edit("changed", NoteKind.Pinned);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync(ct);
        }

        (await RawNoteAsync(id))["text"].AsString.ShouldBe("changed");
    }

    [Fact]
    public async Task WhenOneWriteIsRefusedTheOtherIsNotCommitted()
    {
        var ct = TestContext.Current.CancellationToken;
        var host = Host("clinic-a");
        var existing = Id("all-or-nothing-existing");
        var added = Id("all-or-nothing-added");
        await CommitAsync(host, notes => notes.Add(new Note(existing, "original")));

        using (var scope = host.Services.CreateScope())
        {
            var notes = scope.ServiceProvider.GetRequiredService<NoteWriteRepository>();
            var loaded = await notes.GetAsync(existing, ct);
            loaded!.Edit("changed", NoteKind.Pinned);
            notes.Add(new Note(added, "new"));
            await BumpVersionInsideTheTransactionAsync(scope, existing, ct);

            await Should.ThrowAsync<VersionConflictException>(() => scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync(ct));
        }

        (await RawNotes().CountDocumentsAsync(_filter.Eq("_id", Bin(added)), cancellationToken: ct)).ShouldBe(0);
        (await RawNoteAsync(existing))["text"].AsString.ShouldBe("original");
    }

    [Fact]
    public async Task ALoadedAggregateIsWrittenEvenWhenUnchanged()
    {
        var host = Host("clinic-a");
        var id = Id("unchanged");
        await CommitAsync(host, notes => notes.Add(new Note(id, "same")));

        using (var scope = host.Services.CreateScope())
        {
            _ = await scope.ServiceProvider.GetRequiredService<NoteWriteRepository>().GetAsync(id, TestContext.Current.CancellationToken);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync(TestContext.Current.CancellationToken);
        }

        var document = await RawNoteAsync(id);
        document["version"].AsInt64.ShouldBe(2);
        document["text"].AsString.ShouldBe("same");
    }

    [Fact]
    public async Task ASaveKeepsCreatedAtAndAdvancesVersionAndUpdatedAt()
    {
        var host = Host("clinic-a");
        var id = Id("edited");
        await CommitAsync(host, notes => notes.Add(new Note(id, "before")));
        var before = await RawNoteAsync(id);

        await Task.Delay(20, TestContext.Current.CancellationToken);
        using (var scope = host.Services.CreateScope())
        {
            var note = await scope.ServiceProvider.GetRequiredService<NoteWriteRepository>().GetAsync(id, TestContext.Current.CancellationToken);
            note.ShouldNotBeNull().Text.ShouldBe("before");
            note.Edit("after", NoteKind.Pinned);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync(TestContext.Current.CancellationToken);
        }

        var after = await RawNoteAsync(id);
        after["text"].AsString.ShouldBe("after");
        after["kind"].AsString.ShouldBe("Pinned");
        after["version"].AsInt64.ShouldBe(2);
        after["createdAt"].ToUniversalTime().ShouldBe(before["createdAt"].ToUniversalTime());
        after["updatedAt"].ToUniversalTime().ShouldBeGreaterThan(before["updatedAt"].ToUniversalTime());
    }

    [Fact]
    public async Task AStaleVersionIsRefusedAndNothingIsOverwritten()
    {
        var ct = TestContext.Current.CancellationToken;
        var host = Host("clinic-a");
        var id = Id("stale");
        await CommitAsync(host, notes => notes.Add(new Note(id, "original")));

        using var scope = host.Services.CreateScope();
        var loaded = await scope.ServiceProvider.GetRequiredService<NoteWriteRepository>().GetAsync(id, ct);
        loaded!.Edit("stale write", NoteKind.Plain);
        await BumpVersionInsideTheTransactionAsync(scope, id, ct);

        await Should.ThrowAsync<VersionConflictException>(() => scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync(ct));
        (await RawNoteAsync(id))["text"].AsString.ShouldBe("original");
    }

    [Fact]
    public async Task AChangeAfterTheLoadIsRefusedByTheStore()
    {
        var ct = TestContext.Current.CancellationToken;
        var host = Host("clinic-a");
        var id = Id("concurrent");
        await CommitAsync(host, notes => notes.Add(new Note(id, "original")));

        using var scope = host.Services.CreateScope();
        var loaded = await scope.ServiceProvider.GetRequiredService<NoteWriteRepository>().GetAsync(id, ct);
        loaded!.Edit("loser", NoteKind.Plain);
        await RawNotes().UpdateOneAsync(_filter.Eq("_id", Bin(id)), Builders<BsonDocument>.Update.Set("text", "winner").Inc("version", 1), cancellationToken: ct);

        // Inside the snapshot transaction the store reports a write conflict; a version mismatch is the same refusal.
        var exception = await Should.ThrowAsync<Exception>(() => scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync(ct));
        (exception is VersionConflictException
            || (exception is MongoException mongoException && mongoException.HasErrorLabel("TransientTransactionError")))
            .ShouldBeTrue(exception.ToString());
        (await RawNoteAsync(id))["text"].AsString.ShouldBe("winner");
    }

    [Fact]
    public async Task AnotherTenantsDocumentIsInvisibleAndNeverOverwritten()
    {
        var ct = TestContext.Current.CancellationToken;
        var host = Host("clinic-a");
        var id = Id("foreign");
        await RawNotes().InsertOneAsync(
            new BsonDocument { { "_id", Bin(id) }, { "tenantId", Bin(TenantOf("clinic-b")) }, { "version", 1L }, { "text", "theirs" }, { "kind", "Plain" } },
            cancellationToken: ct);

        using (var scope = host.Services.CreateScope())
        {
            var notes = scope.ServiceProvider.GetRequiredService<NoteWriteRepository>();
            (await notes.GetAsync(id, ct)).ShouldBeNull();
            (await notes.WithTextAsync("theirs", ct)).ShouldBeEmpty();
        }

        // Writing the same id as this tenant fails on the unique _id and leaves the other document alone.
        using (var scope = host.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<NoteWriteRepository>().Add(new Note(id, "mine"));
            var exception = await Should.ThrowAsync<MongoWriteException>(() => scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync(ct));
            exception.WriteError.Category.ShouldBe(ServerErrorCategory.DuplicateKey);
        }

        var stored = await RawNoteAsync(id);
        stored["text"].AsString.ShouldBe("theirs");
        stored["tenantId"].AsGuid.ShouldBe(TenantOf("clinic-b"));
        stored["version"].AsInt64.ShouldBe(1);
    }

    [Fact]
    public async Task TwoTenantsOnOneDatabaseEachSeeOnlyTheirOwnDocuments()
    {
        var ct = TestContext.Current.CancellationToken;
        var hostA = Host("clinic-a");
        var hostB = Host("clinic-b");
        var idA = Id("shared-a");
        var idB = Id("shared-b");
        await CommitAsync(hostA, notes => notes.Add(new Note(idA, "shared text")));
        await CommitAsync(hostB, notes => notes.Add(new Note(idB, "shared text")));

        using var scopeA = hostA.Services.CreateScope();
        using var scopeB = hostB.Services.CreateScope();
        var notesA = scopeA.ServiceProvider.GetRequiredService<NoteWriteRepository>();
        var notesB = scopeB.ServiceProvider.GetRequiredService<NoteWriteRepository>();

        (await notesA.GetAsync(idB, ct)).ShouldBeNull();
        (await notesB.GetAsync(idA, ct)).ShouldBeNull();
        (await notesA.WithTextAsync("shared text", ct)).Select(note => note.Id).ShouldBe([idA]);
        (await notesB.WithTextAsync("shared text", ct)).Select(note => note.Id).ShouldBe([idB]);
    }

    [Fact]
    public async Task AGlobalAggregateHasNoTenantFieldAndIsVisibleToEveryTenant()
    {
        var ct = TestContext.Current.CancellationToken;
        var id = Id("global");
        using (var scope = Host("clinic-a").Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<GlobalNoteWriteRepository>().Add(new GlobalNote(id, "everyone"));
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync(ct);
        }

        var document = await mongo.Database.GetCollection<BsonDocument>(GlobalNote.CollectionName)
            .Find(_filter.Eq("_id", Bin(id))).SingleAsync(ct);
        document.Contains("tenantId").ShouldBeFalse();
        document["version"].AsInt64.ShouldBe(1);

        using var other = Host("clinic-b").Services.CreateScope();
        (await other.ServiceProvider.GetRequiredService<GlobalNoteWriteRepository>().GetAsync(id, ct))!.Text.ShouldBe("everyone");
    }

    [Fact]
    public async Task ALoadedAggregateIsTheSameInstanceWithinOneUnitOfWork()
    {
        var host = Host("clinic-a");
        var id = Id("identity");
        await CommitAsync(host, notes => notes.Add(new Note(id, "identity-text")));

        using var scope = host.Services.CreateScope();
        var notes = scope.ServiceProvider.GetRequiredService<NoteWriteRepository>();
        var first = await notes.GetAsync(id, TestContext.Current.CancellationToken);
        var second = await notes.GetAsync(id, TestContext.Current.CancellationToken);
        var found = await notes.WithTextAsync("identity-text", TestContext.Current.CancellationToken);

        second.ShouldBeSameAs(first);
        found.ShouldHaveSingleItem().ShouldBeSameAs(first);
    }

    [Fact]
    public async Task AUnitOfWorkCommitsOnce()
    {
        var ct = TestContext.Current.CancellationToken;
        using var scope = Host("clinic-a").Services.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        scope.ServiceProvider.GetRequiredService<NoteWriteRepository>().Add(new Note(Id("twice"), "x"));
        await unitOfWork.CommitAsync(ct);

        await Should.ThrowAsync<InvalidOperationException>(() => unitOfWork.CommitAsync(ct));
        await Should.ThrowAsync<InvalidOperationException>(() => scope.ServiceProvider.GetRequiredService<NoteWriteRepository>().GetAsync(Id("twice"), ct));
    }

    [Fact]
    public async Task AddingTheSameAggregateTwiceIsRefused()
    {
        using var scope = Host("clinic-a").Services.CreateScope();
        var notes = scope.ServiceProvider.GetRequiredService<NoteWriteRepository>();
        notes.Add(new Note(Id("added-twice"), "x"));

        Should.Throw<InvalidOperationException>(() => notes.Add(new Note(Id("added-twice"), "y")));
        await Task.CompletedTask;
    }

    [Fact]
    public void TheTransactionUsesMajorityWritesAndSnapshotReads()
    {
        MongoUnitOfWork.TransactionOptions.ReadConcern.ShouldBe(ReadConcern.Snapshot);
        MongoUnitOfWork.TransactionOptions.WriteConcern.ShouldBe(WriteConcern.WMajority);
        MongoUnitOfWork.TransactionOptions.ReadPreference.ShouldBe(ReadPreference.Primary);
    }

    [Fact]
    public async Task TheTransactionRunsWithMajorityAndSnapshotOnTheServer()
    {
        var ct = TestContext.Current.CancellationToken;
        using var scope = Host("clinic-a").Services.CreateScope();
        _ = await scope.ServiceProvider.GetRequiredService<NoteWriteRepository>().GetAsync(Id("options"), ct);

        var session = scope.ServiceProvider.GetRequiredService<MongoUnitOfWork>().Session.ShouldNotBeNull();
        session.IsInTransaction.ShouldBeTrue();
        session.WrappedCoreSession.CurrentTransaction.TransactionOptions.ReadConcern.ShouldBe(ReadConcern.Snapshot);
        session.WrappedCoreSession.CurrentTransaction.TransactionOptions.WriteConcern.ShouldBe(WriteConcern.WMajority);
    }

    [Fact]
    public void ACollectionRefusesAnAggregateThatIsNeitherTenantOwnedNorGlobal()
    {
        var collections = new TenantCollections(mongo.Database, new FixedTenant(TenantOf("clinic-a")));

        var exception = Should.Throw<InvalidOperationException>(() => collections.For<UnmarkedNote>("unmarked"));
        exception.Message.ShouldContain("AD-5");
    }

    private static Guid Id(string name) => DeterministicId.From(DeterministicId.SlotBookNamespace, "test-note:" + name);

    private static Guid TenantOf(string slug) => DeterministicId.ForTenant(slug);

    private static BsonBinaryData Bin(Guid id) => new(id, GuidRepresentation.Standard);

    private static async Task BumpVersionInsideTheTransactionAsync(IServiceScope scope, Guid id, CancellationToken ct)
    {
        var session = scope.ServiceProvider.GetRequiredService<MongoUnitOfWork>().Session.ShouldNotBeNull();
        await scope.ServiceProvider.GetRequiredService<IMongoDatabase>()
            .GetCollection<BsonDocument>(NoteWriteRepository.CollectionName)
            .UpdateOneAsync(session, _filter.Eq("_id", Bin(id)), Builders<BsonDocument>.Update.Inc("version", 1), cancellationToken: ct);
    }

    private static async Task CommitAsync(SlotBookApiFactory host, Action<NoteWriteRepository> work)
    {
        using var scope = host.Services.CreateScope();
        work(scope.ServiceProvider.GetRequiredService<NoteWriteRepository>());
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().CommitAsync(TestContext.Current.CancellationToken);
    }

    private SlotBookApiFactory Host(string tenantSlug)
    {
        var factory = new SlotBookApiFactory { TenantSlug = tenantSlug, MongoDatabase = mongo.DatabaseName };
        _factories.Add(factory);
        return factory;
    }

    private IMongoCollection<BsonDocument> RawNotes() => mongo.Database.GetCollection<BsonDocument>(NoteWriteRepository.CollectionName);

    private async Task<BsonDocument> RawNoteAsync(Guid id) =>
        await RawNotes().Find(_filter.Eq("_id", Bin(id))).SingleAsync(TestContext.Current.CancellationToken);

    private sealed class FixedTenant(Guid tenantId) : ITenantContext
    {
        public Guid TenantId { get; } = tenantId;
    }
}
