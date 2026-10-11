using FluentValidation.Results;
using Shouldly;
using SlotBook.SharedKernel.Application.Validation;
using Xunit;

namespace SlotBook.SharedKernel.Tests.Validation;

public class ValidationCodesTests
{
    [Theory]
    [InlineData("NotEmptyValidator", "required")]
    [InlineData("NotNullValidator", "required")]
    [InlineData("MaximumLengthValidator", "too_long")]
    [InlineData("MinimumLengthValidator", "too_short")]
    [InlineData("LengthValidator", "invalid_length")]
    [InlineData("ExactLengthValidator", "invalid_length")]
    [InlineData("GreaterThanValidator", "out_of_range")]
    [InlineData("GreaterThanOrEqualValidator", "out_of_range")]
    [InlineData("LessThanValidator", "out_of_range")]
    [InlineData("LessThanOrEqualValidator", "out_of_range")]
    [InlineData("InclusiveBetweenValidator", "out_of_range")]
    [InlineData("ExclusiveBetweenValidator", "out_of_range")]
    [InlineData("RegularExpressionValidator", "invalid_format")]
    [InlineData("EmailValidator", "invalid_format")]
    [InlineData("EnumValidator", "invalid_value")]
    [InlineData("StringEnumValidator", "invalid_value")]
    [InlineData("EqualValidator", "invalid_value")]
    [InlineData("NotEqualValidator", "invalid_value")]
    [InlineData("EmptyValidator", "invalid_value")]
    [InlineData("NullValidator", "invalid_value")]
    public void BuiltInCodesMapToOurs(string builtIn, string expected)
    {
        ValidationCodes.Resolve(builtIn).ShouldBe(expected);
        ValidationCodes.All.ShouldContain(expected);
    }

    [Fact]
    public void SnakeCaseCodesPassThrough()
    {
        ValidationCodes.Resolve("slot_in_past").ShouldBe("slot_in_past");
    }

    [Theory]
    [InlineData("PredicateValidator")]
    [InlineData("AsyncPredicateValidator")]
    [InlineData("")]
    [InlineData(null)]
    public void UnmappedCodesThrow(string? code)
    {
        Should.Throw<InvalidOperationException>(() => ValidationCodes.Resolve(code));
    }

    [Theory]
    [InlineData("PatientName", "patientName")]
    [InlineData("Items[0].Name", "items[0].name")]
    [InlineData("Address.Line1", "address.line1")]
    public void FieldNamesAreCamelCasePerSegment(string path, string expected)
    {
        ValidationCodes.ToFieldName(path).ShouldBe(expected);
    }

    [Fact]
    public void ErrorsAreGroupedPerFieldAndDeduplicatedInOrder()
    {
        var failures = new[]
        {
            new ValidationFailure("DisplayName", "x") { ErrorCode = "NotEmptyValidator" },
            new ValidationFailure("DisplayName", "y") { ErrorCode = "MinimumLengthValidator" },
            new ValidationFailure("DisplayName", "z") { ErrorCode = "NotNullValidator" },
            new ValidationFailure("Count", "w") { ErrorCode = "GreaterThanValidator" },
        };

        var errors = ValidationCodes.ToErrors(failures);

        errors["displayName"].ShouldBe(["required", "too_short"]);
        errors["count"].ShouldBe(["out_of_range"]);
    }
}
