using Microsoft.Extensions.Options;

namespace SlotBook.SharedKernel.Infrastructure.Jobs;

internal sealed class JobOptionsValidator : IValidateOptions<JobOptions>
{
    public ValidateOptionsResult Validate(string? name, JobOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.LeaseSeconds < JobOptions.MinimumLeaseSeconds
            ? ValidateOptionsResult.Fail($"{JobOptions.LeaseSecondsKey} must be at least {JobOptions.MinimumLeaseSeconds}")
            : ValidateOptionsResult.Success;
    }
}
