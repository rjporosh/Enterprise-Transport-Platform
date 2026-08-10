# Quartz Jobs — Step-by-Step Guide

This guide explains how Quartz.NET jobs work in the Notification Service and how to create, configure, and monitor them.

## What is Quartz.NET?

Quartz.NET is a job scheduling library for .NET. It handles:
- Cron-based scheduling
- Job persistence
- Misfire handling (what happens when a job should have run but didn't)
- Concurrency control

## Step 1: Understand the Existing Jobs

### NotificationDispatchJob

| Property | Value |
|---|---|
| **Schedule** | Every 10 seconds (`*/10 * * * * ?`) |
| **Concurrency** | `[DisallowConcurrentExecution]` — only one instance at a time |
| **Batch Size** | 50 notifications per run |
| **Purpose** | Picks up Pending/Scheduled/Retrying-due notifications and dispatches them |

### StuckNotificationRecoveryJob

| Property | Value |
|---|---|
| **Schedule** | Every 5 minutes (`0 */5 * * * ?`) |
| **Concurrency** | `[DisallowConcurrentExecution]` |
| **Purpose** | Force-fails notifications stuck in `Sending` for >10 minutes |

## Step 2: How Jobs Are Registered

Jobs are registered in `Infrastructure/Scheduling/QuartzRegistration.cs`:

```csharp
public static IServiceCollection AddNotificationScheduling(this IServiceCollection services)
{
    services.AddQuartz(q =>
    {
        q.UseMicrosoftDependencyInjectionJobFactory();

        // Dispatch job - every 10 seconds
        var dispatchJob = JobBuilder.Create<NotificationDispatchJob>()
            .WithIdentity("notification-dispatch-job", "notification")
            .Build();

        var dispatchTrigger = TriggerBuilder.Create()
            .WithIdentity("notification-dispatch-trigger", "notification")
            .WithCronSchedule("*/10 * * * * ?")
            .Build();

        // Recovery job - every 5 minutes
        var recoveryJob = JobBuilder.Create<StuckNotificationRecoveryJob>()
            .WithIdentity("stuck-notification-recovery-job", "notification")
            .Build();

        var recoveryTrigger = TriggerBuilder.Create()
            .WithIdentity("stuck-notification-recovery-trigger", "notification")
            .WithCronSchedule("0 */5 * * * ?")
            .Build();

        q.AddJob(dispatchJob, trigger => dispatchTrigger);
        q.AddJob(recoveryJob, trigger => recoveryTrigger);
    });

    services.AddQuartzHostedService();
    return services;
}
```

Key points:
- `JobBuilder.Create<T>()` creates a job definition
- `WithIdentity("job-name", "group")` uniquely identifies the job
- `TriggerBuilder.Create()` defines when the job runs
- `WithCronSchedule("...")` sets the schedule using Quartz cron expressions
- `q.AddJob(job, trigger => trigger)` associates a job with its trigger
- `AddQuartzHostedService()` registers Quartz as a hosted service

## Step 3: How to Create a New Job

### Step 3.1: Create the Job Class

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace NotificationService.Infrastructure.Scheduling.Jobs;

/// <summary>
/// Example: Clean up old notification logs older than 90 days.
/// </summary>
[DisallowConcurrentExecution] // Prevents overlapping runs
public sealed class CleanupOldLogsJob : IJob
{
    private const int BatchSize = 1000;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CleanupOldLogsJob> _logger;

    public CleanupOldLogsJob(IServiceScopeFactory scopeFactory, ILogger<CleanupOldLogsJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var correlationId = context.MergedJobDataMap.GetString("CorrelationId") 
            ?? Guid.NewGuid().ToString();
        
        _logger.LogInformation(
            "CleanupOldLogsJob started. CorrelationId={CorrelationId}", 
            correlationId);

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        var dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
        
        var cutoff = dateTimeProvider.UtcNow.AddDays(-90);
        
        var oldLogs = await dbContext.NotificationLogs
            .Where(l => l.AttemptedAtUtc < cutoff)
            .Take(BatchSize)
            .ToListAsync(context.CancellationToken);

        if (oldLogs.Count == 0)
        {
            _logger.LogInformation("No old logs to clean up. CorrelationId={CorrelationId}", correlationId);
            return;
        }

        dbContext.NotificationLogs.RemoveRange(oldLogs);
        await dbContext.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation(
            "Deleted {Count} old notification logs. CorrelationId={CorrelationId}", 
            oldLogs.Count, correlationId);
    }
}
```

### Step 3.2: Register the Job

Add to `QuartzRegistration.cs`:

```csharp
var cleanupJob = JobBuilder.Create<CleanupOldLogsJob>()
    .WithIdentity("cleanup-old-logs-job", "notification")
    .Build();

var cleanupTrigger = TriggerBuilder.Create()
    .WithIdentity("cleanup-old-logs-trigger", "notification")
    .WithCronSchedule("0 0 2 * * ?") // Daily at 2am
    .Build();

q.AddJob(cleanupJob, trigger => cleanupTrigger);
```

### Step 3.3: Verify the Job

Start the service and check logs:

```
[Information] Job 'cleanup-old-logs-job' scheduled with trigger 'cleanup-old-logs-trigger'
```

## Step 4: Cron Expression Reference

### Quartz Cron Format

```
┌───────────── second (0-59)
│ ┌───────────── minute (0-59)
│ │ ┌───────────── hour (0-23)
│ │ │ ┌───────────── day of month (1-31)
│ │ │ │ ┌───────────── month (1-12)
│ │ │ │ │ ┌───────────── day of week (1-7, SUN=1)
│ │ │ │ │ │
* * * * * ?
```

### Common Expressions

| Expression | Description | Use Case |
|---|---|---|
| `*/10 * * * * ?` | Every 10 seconds | Notification dispatch |
| `0 */5 * * * ?` | Every 5 minutes | Recovery/health checks |
| `0 0 * * * ?` | Every hour at :00 | Hourly cleanup |
| `0 0 2 * * ?` | Daily at 2am | Daily maintenance |
| `0 0 8 * * ?` | Daily at 8am | Morning batch |
| `0 0 8 * * MON-FRI ?` | Weekdays at 8am | Business hours |
| `0 0 0 1 * ?` | First of month at midnight | Monthly reports |

### Testing Cron Expressions

Use https://crontab.guru/ (note: Quartz adds a seconds field).

## Step 5: Test Jobs Locally

### Option A: Wait for the Schedule

For a job running every 10 seconds, just watch the logs.

### Option B: Add a Development Trigger

In `Program.cs`, add a manual trigger for testing:

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapPost("/dev/trigger/dispatch", async (IScheduler scheduler) =>
    {
        await scheduler.TriggerJob(new JobKey("notification-dispatch-job", "notification"));
        return Results.Ok("Dispatch job triggered");
    });
}
```

### Option C: Use Quartz Descheduler

For ad-hoc testing, temporarily change the cron to fire immediately:

```csharp
.WithCronSchedule("* * * * * ?") // Every minute for testing
```

## Step 6: Monitor Job Execution

### Logs

Jobs log structured information:

```
NotificationDispatchJob picked up 5 notification(s) to send.
```

### Metrics to Track

| Metric | Where to Find |
|---|---|
| Job execution duration | Logs or OpenTelemetry |
| Notifications sent per run | `Notification` table + logs |
| Failed sends per run | `NotificationLog` + logs |
| Job failures | `logs/runtime-errors/` |
| Stuck notifications recovered | `Notification` status changes |

## Step 7: Production Considerations

### Misfire Policy

By default, Quartz uses `SmartPolicy` — if a trigger misses its fire time, it fires as soon as possible.

To customize:

```csharp
var trigger = TriggerBuilder.Create()
    .WithIdentity("my-trigger")
    .WithCronSchedule("0 */5 * * * ?", x => x
        .WithMisfireHandlingInstructionFireAndProceed())
    .Build();
```

Options:
- `FireAndProceed` — fire immediately, then continue with next scheduled time
- `DoNothing` — skip this fire, wait for next scheduled time

### Concurrency

Always use `[DisallowConcurrentExecution]` on jobs that must not overlap:

```csharp
[DisallowConcurrentExecution]
public sealed class MyJob : IJob
{
    // ...
}
```

### Graceful Shutdown

Quartz jobs receive a cancellation token when the app is shutting down:

```csharp
public async Task Execute(IJobExecutionContext context)
{
    // Check context.CancellationToken
    // Finish current work or stop gracefully
}
```

## Troubleshooting

### Job Not Running

1. Check logs for "Job 'X' scheduled with trigger 'Y'"
2. Verify the cron expression is correct
3. Check that the job class is registered in `QuartzRegistration.cs`
4. Ensure `AddQuartzHostedService()` is called

### Job Running But Not Doing Work

1. Check if there's data to process (e.g., no Pending notifications)
2. Check for exceptions in `logs/runtime-errors/`
3. Verify the batch size isn't too small

### Job Overlapping

1. Ensure `[DisallowConcurrentExecution]` is on the job class
2. Check if the job takes longer than its interval (e.g., 10s job takes 15s)
3. Consider increasing the interval or optimizing the job
