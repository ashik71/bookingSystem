using Shouldly;
using SlotBook.SharedKernel.Infrastructure.Jobs;
using Xunit;

namespace SlotBook.SharedKernel.Tests;

public class HeartbeatTicksTests
{
    private static readonly TimeSpan _minute = TimeSpan.FromMinutes(1);

    [Fact]
    public void TheFirstRunEverRecordsOnlyTheCurrentTick()
    {
        var (due, skipped) = HeartbeatTicks.Due(null, At(7, 31), _minute, 1440);

        due.ShouldBe([At(7)]);
        skipped.ShouldBe(0);
    }

    [Fact]
    public void EveryAlignedTickAfterTheWatermarkUpToNowIsDue()
    {
        var (due, skipped) = HeartbeatTicks.Due(At(3), At(7, 5), _minute, 1440);

        due.ShouldBe([At(4), At(5), At(6), At(7)]);
        skipped.ShouldBe(0);
    }

    [Fact]
    public void NothingIsDueMidPeriodAfterTheLastTick()
    {
        var (due, skipped) = HeartbeatTicks.Due(At(7), At(7, 59), _minute, 1440);

        due.ShouldBeEmpty();
        skipped.ShouldBe(0);
    }

    [Fact]
    public void ATickExactlyAtNowIsDue()
    {
        HeartbeatTicks.Due(At(6), At(7), _minute, 1440).Due.ShouldBe([At(7)]);
    }

    [Fact]
    public void AMinuteGapWithAOneSecondPeriodGivesSixtyTicks()
    {
        var second = TimeSpan.FromSeconds(1);

        var (due, _) = HeartbeatTicks.Due(At(0), At(1), second, 1440);

        due.Count.ShouldBe(60);
        due[0].ShouldBe(At(0, 1));
        due[^1].ShouldBe(At(1));
    }

    [Fact]
    public void ATenSecondPeriodGivesSixTicksInAMinute()
    {
        HeartbeatTicks.Due(At(0), At(1), TimeSpan.FromSeconds(10), 1440).Due.Count.ShouldBe(6);
    }

    [Fact]
    public void TheCapKeepsTheNewestTicksAndCountsTheSkippedOnes()
    {
        var (due, skipped) = HeartbeatTicks.Due(At(0), At(10), _minute, 3);

        due.ShouldBe([At(8), At(9), At(10)]);
        skipped.ShouldBe(7);
    }

    [Fact]
    public void ANonUtcWatermarkIsReadAsTheSameMoment()
    {
        var local = new DateTime(2026, 1, 1, 10, 3, 0, DateTimeKind.Utc).ToLocalTime();

        HeartbeatTicks.Due(local, At(5), _minute, 1440).Due.ShouldBe([At(4), At(5)]);
    }

    [Fact]
    public void TheValidatorAcceptsTheHeartbeatDefaults()
    {
        new JobOptionsValidator().Validate(null, new JobOptions()).Succeeded.ShouldBeTrue();
        new JobOptions().HeartbeatEnabled.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0, 1440, "Jobs:Heartbeat:PeriodSeconds")]
    [InlineData(-1, 1440, "Jobs:Heartbeat:PeriodSeconds")]
    [InlineData(60, 0, "Jobs:Heartbeat:MaxTicksPerRun")]
    [InlineData(60, -5, "Jobs:Heartbeat:MaxTicksPerRun")]
    public void ANonPositivePeriodOrCapFails(int period, int cap, string key)
    {
        var result = new JobOptionsValidator().Validate(null, new JobOptions { HeartbeatPeriodSeconds = period, HeartbeatMaxTicksPerRun = cap });

        result.Failed.ShouldBeTrue();
        result.FailureMessage!.ShouldContain(key);
    }

    private static DateTime At(int minute, int second = 0) => new(2026, 1, 1, 10, minute, second, DateTimeKind.Utc);
}
