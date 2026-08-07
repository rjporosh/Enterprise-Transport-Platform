using Microsoft.Extensions.DependencyInjection;
using NotificationService.Infrastructure.Scheduling.Jobs;
using Quartz;

namespace NotificationService.Infrastructure.Scheduling;

/// <summary>
/// Cron expressions used (standard Quartz 6-field: sec min hour day month
/// weekday), documented here rather than scattered across the codebase —
/// also mirrored in docs/programmers-guide/quartz-jobs.md:
///   NotificationDispatchJob:        every 10 seconds  -&gt; "*/10 * * * * ?"
///   StuckNotificationRecoveryJob:   every 5 minutes    -&gt; "0 */5 * * * ?"
/// </summary>
public static class QuartzRegistration
{
    public static IServiceCollection AddNotificationScheduling(this IServiceCollection services)
    {
        services.AddQuartz(quartz =>
        {
            var dispatchJobKey = new JobKey(nameof(NotificationDispatchJob));
            quartz.AddJob<NotificationDispatchJob>(opts => opts.WithIdentity(dispatchJobKey));
            quartz.AddTrigger(opts => opts
                .ForJob(dispatchJobKey)
                .WithIdentity($"{nameof(NotificationDispatchJob)}-trigger")
                .WithCronSchedule("*/10 * * * * ?"));

            var recoveryJobKey = new JobKey(nameof(StuckNotificationRecoveryJob));
            quartz.AddJob<StuckNotificationRecoveryJob>(opts => opts.WithIdentity(recoveryJobKey));
            quartz.AddTrigger(opts => opts
                .ForJob(recoveryJobKey)
                .WithIdentity($"{nameof(StuckNotificationRecoveryJob)}-trigger")
                .WithCronSchedule("0 */5 * * * ?"));
        });

        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

        return services;
    }
}
