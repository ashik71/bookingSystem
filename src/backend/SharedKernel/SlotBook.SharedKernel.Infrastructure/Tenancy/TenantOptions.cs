namespace SlotBook.SharedKernel.Infrastructure.Tenancy;

/// <summary>Bound from <c>Tenancy:Tenant</c>.</summary>
internal sealed class TenantOptions
{
    public const string SectionName = "Tenancy:Tenant";

    public string? Slug { get; set; }

    public string? DisplayName { get; set; }

    /// <summary>ISO 3166-1 alpha-2, upper case.</summary>
    public string? Country { get; set; }

    /// <summary>IANA time zone id.</summary>
    public string? TimeZone { get; set; }

    public DayOfWeek? WeekStartDay { get; set; }
}
