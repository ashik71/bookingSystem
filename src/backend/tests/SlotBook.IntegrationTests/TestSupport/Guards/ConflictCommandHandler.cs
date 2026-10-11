#pragma warning disable CA1812 // instantiated by the dispatcher scan
using SlotBook.SharedKernel.Application;
using SlotBook.SharedKernel.Application.Dispatching;
using SlotBook.SharedKernel.Infrastructure.Persistence;

namespace SlotBook.IntegrationTests.TestSupport.Guards;

internal sealed class ConflictCommandHandler(ConflictAttempts attempts) : ICommandHandler<ConflictCommand, int>
{
    public Task<Result<int>> HandleAsync(ConflictCommand command, CancellationToken cancellationToken)
    {
        var attempt = attempts.Next(command.Key);
        if (command.RefuseOnRerun && attempt > 1)
        {
            return Task.FromResult<Result<int>>(Result.Refused("counter_full"));
        }

        throw new VersionConflictException(GuardCounterWriteRepository.CollectionName, Guid.Empty);
    }
}
