#pragma warning disable CA1812 // instantiated by the dispatcher scan
using SlotBook.SharedKernel.Application;
using SlotBook.SharedKernel.Application.Dispatching;

namespace SlotBook.IntegrationTests.TestSupport;

internal sealed class CreateProbeCommandHandler(HandlerProbe probe) : ICommandHandler<CreateProbeCommand, ProbeCreated>
{
    public Task<Result<ProbeCreated>> HandleAsync(CreateProbeCommand command, CancellationToken cancellationToken)
    {
        probe.Ran();
        return Task.FromResult<Result<ProbeCreated>>(new ProbeCreated(command.DisplayName, command.Count));
    }
}
