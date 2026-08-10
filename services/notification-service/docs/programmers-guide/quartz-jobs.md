# Quartz Jobs

## Registration

Jobs are registered in `Infrastructure/Scheduling/QuartzRegistration.cs`:

```csharp
public static IServiceCollection AddNotificationScheduling(this IServiceCollection services)
{
    services.AddQuartz(q =>
    {
        q.UseMicrosoftDependencyInjectionJobFactory();

        var dispatchJob = JobBuilder.Create<NotificationDispatchJob>()
            .WithIdentity("notification-dispatch-job", "notification")
            .Build();

        var dispatchTrigger = TriggerBuilder.Create()
            .WithIdentity("notification-dispatch-trigger", "notification")
            .WithCronSchedule("*/10 * * * * ?") // every 10 seconds
            .Build();

        var recoveryJob = JobBuilder.Create<StuckNotificationRecoveryJob>()
            .WithIdentity("stuck-notification-recovery-job", "notification")
            .Build();

        var recoveryTrigger = TriggerBuilder.Create()
            .WithIdentity("stuck-notification-recovery-trigger", "notification")
            .WithCronSchedule("0 */5 * * * ?") // every 5 minutes
            .Build();

        q.AddJob(dispatchJob, trigger => dispatchTrigger);
        q.AddJob(recoveryJob, trigger => recoveryTrigger);
    });

    services.AddQuartzHostedService();
    return services;
}
```

## Existing Jobs

| Job | Trigger | Purpose |
|---|---|---|
| `NotificationDispatchJob` | Every 10s | Picks up Pending/Scheduled/Retrying-due notifications and dispatches them |
| `StuckNotificationRecoveryJob` | Every 5 min | Force-fails notifications stuck in Sending for >10 min |

## Creating a New Job

1. Implement `IJob` with `[DisallowConcurrentExecution]` attribute
2. Resolve scoped services via `IServiceScopeFactory`
3. Register in `QuartzRegistration.cs`
4. Document in this guide

## Cron Expressions

| Expression | Meaning |
|---|---|
| `*/10 * * * * ?` | Every 10 seconds |
| `0 */5 * * * ?` | Every 5 minutes |
| `0 0 * * * ?` | Every hour |
| `0 0 8 * * ?` | Every day at 8am |
