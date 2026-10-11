using Shouldly;
using SlotBook.SharedKernel.Application;
using Xunit;

namespace SlotBook.SharedKernel.Tests;

public class ResultTests
{
    [Fact]
    public void ValueConvertsImplicitlyToSuccess()
    {
        Result<int> result = 5;

        result.IsRefused.ShouldBeFalse();
        result.Value.ShouldBe(5);
        result.Code.ShouldBeNull();
    }

    [Fact]
    public void RefusedConvertsToAnyResultAndCarriesCode()
    {
        Result<string> text = Result.Refused("slot_full");
        Result<int> number = Result.Refused("slot_full");

        text.IsRefused.ShouldBeTrue();
        text.Code.ShouldBe("slot_full");
        number.Code.ShouldBe("slot_full");
    }

    [Fact]
    public void ValueOnARefusalThrows()
    {
        Result<int> result = Result.Refused("slot_full");

        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("SlotFull")]
    [InlineData("slot-full")]
    [InlineData("slot__full")]
    [InlineData("_slot")]
    [InlineData("slot_")]
    public void RefusedRejectsNonSnakeCaseCodes(string code)
    {
        Should.Throw<ArgumentException>(() => Result.Refused(code));
    }
}
