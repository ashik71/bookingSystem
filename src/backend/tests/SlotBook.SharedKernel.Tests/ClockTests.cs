using Shouldly;
using SlotBook.SharedKernel.Application;
using SlotBook.SharedKernel.Infrastructure.Time;
using Xunit;

namespace SlotBook.SharedKernel.Tests;

public class ClockTests
{
    [Fact]
    public void UtcNowReturnsTheTimeProvidersInstantWithZeroOffset()
    {
        var instant = new DateTimeOffset(2026, 10, 11, 8, 30, 0, TimeSpan.FromHours(2));
        IClock clock = new Clock(new FixedTimeProvider(instant));

        clock.UtcNow.ShouldBe(instant);
        clock.UtcNow.Offset.ShouldBe(TimeSpan.Zero);
    }

    private sealed class FixedTimeProvider(DateTimeOffset instant) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => instant.ToUniversalTime();
    }
}
