using Shouldly;
using SlotBook.SharedKernel.Infrastructure.Jobs;
using Xunit;

namespace SlotBook.SharedKernel.Tests;

public class JobOptionsValidatorTests
{
    private static readonly JobOptionsValidator _validator = new();

    [Fact]
    public void TheDefaultPasses()
    {
        _validator.Validate(null, new JobOptions()).Succeeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData(3)]
    [InlineData(600)]
    public void ALeaseOfThreeSecondsOrMorePasses(int seconds)
    {
        _validator.Validate(null, new JobOptions { LeaseSeconds = seconds }).Succeeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData(-5)]
    [InlineData(0)]
    [InlineData(2)]
    public void ALeaseShorterThanThreeSecondsFails(int seconds)
    {
        var result = _validator.Validate(null, new JobOptions { LeaseSeconds = seconds });

        result.Failed.ShouldBeTrue();
        result.FailureMessage!.ShouldContain("Jobs:LeaseSeconds");
    }
}
