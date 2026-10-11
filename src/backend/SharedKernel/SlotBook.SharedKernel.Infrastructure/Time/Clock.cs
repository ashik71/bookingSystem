using SlotBook.SharedKernel.Application;

namespace SlotBook.SharedKernel.Infrastructure.Time;

internal sealed class Clock(TimeProvider timeProvider) : IClock
{
    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();
}
