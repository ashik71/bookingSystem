namespace SlotBook.SharedKernel.Application;

/// <summary>Error codes shared by every module (AD-13). Codes are globally unique snake_case.</summary>
public static class CommonErrors
{
    public const string NotFound = "not_found";
    public const string ValidationFailed = "validation_failed";
    public const string ConcurrencyConflict = "concurrency_conflict";
    public const string RateLimited = "rate_limited";
    public const string XsrfInvalid = "xsrf_invalid";
    public const string InternalError = "internal_error";
}
