using Shouldly;
using SlotBook.SharedKernel.Infrastructure.Tenancy;
using Xunit;

namespace SlotBook.SharedKernel.Tests;

public class TenantOptionsValidatorTests
{
    private static readonly TenantOptionsValidator _validator = new();

    [Fact]
    public void ValidOptionsPass()
    {
        _validator.Validate(null, Valid()).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void EmptySectionNamesEveryKey()
    {
        var result = _validator.Validate(null, new TenantOptions());

        result.Failed.ShouldBeTrue();
        foreach (var key in new[] { "Slug", "DisplayName", "Country", "TimeZone", "WeekStartDay" })
        {
            result.FailureMessage!.ShouldContain($"Tenancy:Tenant:{key} is required");
        }
    }

    [Theory]
    [InlineData("Slug", "Demo-Clinic")]
    [InlineData("Slug", "-demo")]
    [InlineData("Slug", "demo--clinic")]
    [InlineData("Country", "GBR")]
    [InlineData("Country", "gb")]
    [InlineData("TimeZone", "Mars/Olympus")]
    [InlineData("TimeZone", "Pacific Standard Time")]
    public void InvalidValueNamesItsKey(string key, string value)
    {
        var options = Valid();
        switch (key)
        {
            case "Slug": options.Slug = value; break;
            case "Country": options.Country = value; break;
            default: options.TimeZone = value; break;
        }

        var result = _validator.Validate(null, options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage!.ShouldContain($"Tenancy:Tenant:{key}");
    }

    [Fact]
    public void SlugLongerThan63CharactersFails()
    {
        var options = Valid();
        options.Slug = new string('a', 64);

        _validator.Validate(null, options).FailureMessage!.ShouldContain("Tenancy:Tenant:Slug");
    }

    [Fact]
    public void UndefinedWeekStartDayFails()
    {
        var options = Valid();
        options.WeekStartDay = (DayOfWeek)42;

        _validator.Validate(null, options).FailureMessage!.ShouldContain("Tenancy:Tenant:WeekStartDay");
    }

    private static TenantOptions Valid() => new()
    {
        Slug = "demo-clinic",
        DisplayName = "Demo Clinic",
        Country = "GB",
        TimeZone = "Europe/London",
        WeekStartDay = DayOfWeek.Monday,
    };
}
