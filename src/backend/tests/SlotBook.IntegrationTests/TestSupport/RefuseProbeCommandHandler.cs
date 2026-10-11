#pragma warning disable CA1812 // instantiated by the dispatcher scan
using SlotBook.SharedKernel.Application;
using SlotBook.SharedKernel.Application.Dispatching;

namespace SlotBook.IntegrationTests.TestSupport;

internal sealed class RefuseProbeCommandHandler : ICommandHandler<RefuseProbeCommand, ProbeCreated>
{
    public Task<Result<ProbeCreated>> HandleAsync(RefuseProbeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Task.FromResult<Result<ProbeCreated>>(Result.Refused(command.Code));
    }
}
