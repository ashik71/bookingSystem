#pragma warning disable CA1812 // instantiated by the dispatcher scan
using MongoDB.Bson;
using MongoDB.Driver;
using SlotBook.IntegrationTests.TestSupport.Guards;
using SlotBook.IntegrationTests.TestSupport.Persistence;
using SlotBook.SharedKernel.Application;
using SlotBook.SharedKernel.Application.Dispatching;

namespace SlotBook.IntegrationTests.TestSupport.Messaging;

internal sealed class CreateNoteWithEventCommandHandler(
    NoteWriteRepository notes,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    IClock clock,
    IMongoDatabase database,
    ConflictAttempts attempts) : ICommandHandler<CreateNoteWithEventCommand, Guid>
{
    public async Task<Result<Guid>> HandleAsync(CreateNoteWithEventCommand command, CancellationToken cancellationToken)
    {
        var attempt = attempts.Next(command.NoteId.ToString());
        if (command.Mode == CreateNoteMode.ConflictOnce)
        {
            // Load the note, so the commit checks its version.
            _ = await notes.GetAsync(command.ExistingNoteId, cancellationToken).ConfigureAwait(false);
        }

        notes.Add(new Note(command.NoteId, command.Text));
        var stamp = EventStamp.Now(clock);
        unitOfWork.AddIntegrationEvent(new NoteCreated
        {
            EventId = stamp.EventId,
            TenantId = tenant.TenantId,
            OccurredAt = stamp.OccurredAt,
            NoteId = command.NoteId,
            Text = command.Text,
        });

        if (command.Mode == CreateNoteMode.ThrowAfterStaging)
        {
            throw new InvalidOperationException("The handler failed after staging an event.");
        }

        if (command.Mode == CreateNoteMode.ConflictOnce && attempt == 1)
        {
            await database.GetCollection<BsonDocument>(NoteWriteRepository.CollectionName).UpdateOneAsync(
                Builders<BsonDocument>.Filter.Eq("_id", new BsonBinaryData(command.ExistingNoteId, GuidRepresentation.Standard)),
                Builders<BsonDocument>.Update.Inc("version", 1),
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
        return command.NoteId;
    }
}
