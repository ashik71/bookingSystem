#pragma warning disable CA1812 // instantiated by the dispatcher scan
using SlotBook.SharedKernel.Application;
using SlotBook.SharedKernel.Application.Dispatching;
using SlotBook.SharedKernel.Domain;
using SlotBook.SharedKernel.Infrastructure.Persistence;

namespace SlotBook.IntegrationTests.TestSupport.Messaging;

/// <summary>Get-or-create the counter of the note, increment it and commit, the way a real handler would.</summary>
internal sealed class RecordNoteSeenCommandHandler(
    NoteSeenWriteRepository seen,
    IUnitOfWork unitOfWork,
    ITenantContext tenant,
    IClock clock,
    ConsumerProbe probe) : ICommandHandler<RecordNoteSeenCommand, int>
{
    public async Task<Result<int>> HandleAsync(RecordNoteSeenCommand command, CancellationToken cancellationToken)
    {
        var attempt = probe.NextAttempt(command.NoteId);
        switch (command.Behavior)
        {
            case ConsumerBehavior.DomainFailure:
                throw new DomainException("note_not_acceptable");
            case ConsumerBehavior.TransientFailureOnce when attempt == 1:
                throw new TimeoutException("A transient failure.");
            case ConsumerBehavior.ConflictOnce when attempt == 1:
                throw new VersionConflictException("note_seen", command.NoteId);
        }

        var id = DeterministicId.ForGuard(tenant.TenantId, "note-seen", command.NoteId.ToString());
        var counter = await seen.GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (counter is null)
        {
            counter = new NoteSeen(id);
            seen.Add(counter);
        }

        counter.Increment();
        if (command.Behavior == ConsumerBehavior.StageFollowUp)
        {
            var stamp = EventStamp.Now(clock);
            unitOfWork.AddIntegrationEvent(new NoteArchived
            {
                EventId = stamp.EventId,
                TenantId = tenant.TenantId,
                OccurredAt = stamp.OccurredAt,
                NoteId = command.NoteId,
                Tags = [],
                Kind = "follow-up",
            });
        }

        await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false);
        return command.Behavior == ConsumerBehavior.Refuse ? Result.Refused("note_refused") : counter.Count;
    }
}
