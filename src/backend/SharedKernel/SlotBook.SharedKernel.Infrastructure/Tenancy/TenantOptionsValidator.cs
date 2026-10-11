using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace SlotBook.SharedKernel.Infrastructure.Tenancy;

internal sealed partial class TenantOptionsValidator : IValidateOptions<TenantOptions>
{
    private const int MaxSlugLength = 63;
    private const string Prefix = TenantOptions.SectionName + ":";

    public ValidateOptionsResult Validate(string? name, TenantOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Slug))
        {
            failures.Add($"{Prefix}Slug is required");
        }
        else if (options.Slug.Length > MaxSlugLength || !SlugPattern().IsMatch(options.Slug))
        {
            failures.Add($"{Prefix}Slug must be lower-case kebab-case, at most {MaxSlugLength} characters");
        }

        if (string.IsNullOrWhiteSpace(options.DisplayName))
        {
            failures.Add($"{Prefix}DisplayName is required");
        }

        if (string.IsNullOrWhiteSpace(options.Country))
        {
            failures.Add($"{Prefix}Country is required");
        }
        else if (!CountryPattern().IsMatch(options.Country))
        {
            failures.Add($"{Prefix}Country must be two upper-case letters (ISO 3166-1 alpha-2)");
        }

        if (string.IsNullOrWhiteSpace(options.TimeZone))
        {
            failures.Add($"{Prefix}TimeZone is required");
        }
        else if (!IsIanaZone(options.TimeZone))
        {
            failures.Add($"{Prefix}TimeZone must be a known IANA time zone id");
        }

        if (options.WeekStartDay is null)
        {
            failures.Add($"{Prefix}WeekStartDay is required");
        }
        else if (!Enum.IsDefined(options.WeekStartDay.Value))
        {
            failures.Add($"{Prefix}WeekStartDay must be a day of the week");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static bool IsIanaZone(string id)
    {
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(id, out _))
        {
            return false;
        }

        // IANA ids are Area/Location (or UTC); Windows ids such as "Pacific Standard Time" are rejected.
        return id == "UTC" || (id.Contains('/', StringComparison.Ordinal) && !id.Contains(' ', StringComparison.Ordinal));
    }

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 100)]
    private static partial Regex SlugPattern();

    [GeneratedRegex("^[A-Z]{2}$", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 100)]
    private static partial Regex CountryPattern();
}
