using Microsoft.Extensions.Options;

namespace SlotBook.SharedKernel.Infrastructure.Jobs;

internal sealed class JobOptionsValidator : IValidateOptions<JobOptions>
{
    public ValidateOptionsResult Validate(string? name, JobOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();
        if (options.LeaseSeconds < JobOptions.MinimumLeaseSeconds)
        {
            failures.Add($"{JobOptions.LeaseSecondsKey} must be at least {JobOptions.MinimumLeaseSeconds}");
        }

        if (options.HeartbeatPeriodSeconds <= 0)
        {
            failures.Add($"{JobOptions.HeartbeatPeriodSecondsKey} must be positive");
        }

        if (options.HeartbeatMaxTicksPerRun <= 0)
        {
            failures.Add($"{JobOptions.HeartbeatMaxTicksPerRunKey} must be positive");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
