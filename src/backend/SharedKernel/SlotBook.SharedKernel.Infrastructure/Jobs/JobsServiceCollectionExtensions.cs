using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace SlotBook.SharedKernel.Infrastructure.Jobs;

public static class JobsServiceCollectionExtensions
{
    /// <summary>
    /// Registers the job options (<c>Jobs:*</c>, validated on start) and, when <c>Jobs:Heartbeat:Enabled</c> is set, the heartbeat job. Jobs are added with <see cref="AddTimedJob{TJob}"/>.
    /// Call it after <c>AddSharedKernel</c>, which registers the lease store and the MongoDB database.
    /// </summary>
    public static IServiceCollection AddSlotBookJobs(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<JobOptions>()
            .Configure(options =>
            {
                options.LeaseSeconds = configuration.GetValue(JobOptions.LeaseSecondsKey, options.LeaseSeconds);
                options.HeartbeatEnabled = configuration.GetValue(JobOptions.HeartbeatEnabledKey, options.HeartbeatEnabled);
                options.HeartbeatPeriodSeconds = configuration.GetValue(JobOptions.HeartbeatPeriodSecondsKey, options.HeartbeatPeriodSeconds);
                options.HeartbeatMaxTicksPerRun = configuration.GetValue(JobOptions.HeartbeatMaxTicksPerRunKey, options.HeartbeatMaxTicksPerRun);
            })
            .ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<JobOptions>, JobOptionsValidator>());
        if (configuration.GetValue(JobOptions.HeartbeatEnabledKey, false))
        {
            services.AddTimedJob<HeartbeatJob>();
        }

        return services;
    }

    /// <summary>
    /// Registers <typeparamref name="TJob"/> as a singleton timed job with its runner (a hosted service). Each run takes an
    /// expiring lease first, so only one host runs it at a time. The job must be idempotent and catch up.
    /// </summary>
    public static IServiceCollection AddTimedJob<TJob>(this IServiceCollection services)
        where TJob : class, ITimedJob
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<TJob>();
        services.AddSingleton<JobRunner>(provider =>
            ActivatorUtilities.CreateInstance<JobRunner>(provider, (ITimedJob)provider.GetRequiredService<TJob>()));
        services.AddSingleton<IHostedService>(provider =>
            provider.GetServices<JobRunner>().Single(runner => runner.Job is TJob));
        return services;
    }
}
