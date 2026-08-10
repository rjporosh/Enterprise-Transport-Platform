# Background Workers & Cron Jobs

This guide explains how background workers and scheduled jobs work in the Notification Service, and how to add new ones.

## Architecture Overview

The Notification Service uses two background processing mechanisms:

1. **Quartz.NET** — time-based scheduled jobs (dispatch, recovery)
2. **BackgroundService** — long-running processes (outbox processor, RabbitMQ consumer)

```
┌─────────────────────────────────────────┐
│           Hosted Services                │
├─────────────────────────────────────────┤
│  OutboxProcessor (every 5s)             │
│  NotificationEventConsumer (continuous) │
│  QuartzHostedService                     │
│    ├── NotificationDispatchJob (10s)     │
│    └── StuckNotificationRecoveryJob (5m) │
└─────────────────────────────────────────┘
```

## Step-by-Step: Understanding Existing Background Workers

### 1. OutboxProcessor (BackgroundService)

**Purpose**: Polls the `outbox_messages` table and relays unprocessed events to RabbitMQ.

**Configuration**:
- Poll interval: 5 seconds
- Batch size: 100 messages
- Max retries: 5

**How it works**:
1. Queries `outbox_messages` where `ProcessedOnUtc == null AND RetryCount < 5`
2. Publishes each message to RabbitMQ
3. Sets `ProcessedOnUtc` on success
4. Increments `RetryCount` on failure
5. Sleeps 5 seconds, repeats

**Location**: `src/NotificationService.Infrastructure/Persistence/Outbox/OutboxProcessor.cs`

### 2. NotificationEventConsumer (BackgroundService)

**Purpose**: Subscribes to upstream RabbitMQ exchanges and transforms events into notifications.

**How it works**:
1. On startup, declares a durable queue bound to configured upstream exchanges
2. Listens for messages
3. On message received:
   - Extracts recipient from event payload
   - Resolves template
   - Sends `SendNotificationCommand` via MediatR
   - Acks the message
4. On failure: requeues once, then acks-and-drops

**Location**: `src/NotificationService.Infrastructure/Messaging/NotificationEventConsumer.cs`

### 3. NotificationDispatchJob (Quartz Job)

**Purpose**: The single dispatch point for all outbound sends.

**Schedule**: Every 10 seconds (`*/10 * * * * ?`)

**How it works**:
1. Picks up notifications where:
   - `Status = Pending`, OR
   - `Status = Scheduled AND ScheduledForUtc <= now`, OR
   - `Status = Retrying AND NextRetryAtUtc <= now`
2. Orders by `Priority DESC`, `CreatedAtUtc ASC`
3. Batches 50 notifications per run
4. For each notification:
   - Calls `MarkSending()`
   - Invokes the appropriate channel sender (Email/SMS/Push)
   - Calls `MarkSent()` on success or `MarkFailed()` on failure
5. Saves all changes in one transaction

**Location**: `src/NotificationService.Infrastructure/Scheduling/Jobs/NotificationDispatchJob.cs`

### 4. StuckNotificationRecoveryJob (Quartz Job)

**Purpose**: Safety net for notifications stuck in `Sending` state due to process crashes.

**Schedule**: Every 5 minutes (`0 */5 * * * ?`)

**How it works**:
1. Finds notifications where `Status = Sending AND UpdatedAtUtc < now - 10 minutes`
2. Calls `MarkFailed()` with a descriptive error
3. The notification re-enters the retry queue

**Location**: `src/NotificationService.Infrastructure/Scheduling/Jobs/StuckNotificationRecoveryJob.cs`

## Step-by-Step: Adding a New Quartz Job

### Step 1: Create the Job Class

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace NotificationService.Infrastructure.Scheduling.Jobs;

/// <summary>
/// Cleans up old notification logs (older than 90 days).
/// </summary>
[DisallowConcurrentExecution] // Prevents overlapping runs
public sealed class CleanupOldNotificationLogsJob : IJob
{
    private const int BatchSize = 1000;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CleanupOldNotificationLogsJob> _logger;

    public CleanupOldNotificationLogsJob(IServiceScopeFactory scopeFactory, ILogger<CleanupOldNotificationLogsJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var correlationId = context.MergedJobDataMap.GetString("CorrelationId") ?? Guid.NewGuid().ToString();
        
        _logger.LogInformation("CleanupOldNotificationLogsJob started. CorrelationId={CorrelationId}", correlationId);

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

        _logger.LogInformation("Deleted {Count} old notification logs. CorrelationId={CorrelationId}", oldLogs.Count, correlationId);
    }
}
```

### Step 2: Register the Job in QuartzRegistration.cs

```csharp
public static IServiceCollection AddNotificationScheduling(this IServiceCollection services)
{
    services.AddQuartz(q =>
    {
        q.UseMicrosoftDependencyInjectionJobFactory();

        // ... existing jobs ...

        var cleanupJob = JobBuilder.Create<CleanupOldNotificationLogsJob>()
            .WithIdentity("cleanup-old-logs-job", "notification")
            .Build();

        var cleanupTrigger = TriggerBuilder.Create()
            .WithIdentity("cleanup-old-logs-trigger", "notification")
            .WithCronSchedule("0 0 2 * * ?") // Daily at 2am
            .Build();

        q.AddJob(cleanupJob, trigger => cleanupTrigger);
    });

    services.AddQuartzHostedService();
    return services;
}
```

### Step 3: Add Observability

Log structured information in every job:
- Job name
- Trigger name
- CorrelationId (from `IJobExecutionContext`)
- Start/end time
- Duration
- Items processed
- Errors

## Step-by-Step: Adding a New BackgroundService

### Step 1: Create the Service Class

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NotificationService.Infrastructure.Workers;

public sealed class TemplateWarmupWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TemplateWarmupWorker> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    public TemplateWarmupWorker(IServiceScopeFactory scopeFactory, ILogger<TemplateWarmupWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TemplateWarmupWorker starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DoWorkAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TemplateWarmupWorker failed. Will retry after interval.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task DoWorkAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        
        var activeTemplates = await dbContext.NotificationTemplates
            .Where(t => t.IsActive && !t.IsDeleted)
            .ToListAsync(cancellationToken);
        
        _logger.LogInformation("Warmed up {Count} active templates.", activeTemplates.Count);
    }
}
```

### Step 2: Register in DependencyInjection.cs

```csharp
services.AddHostedService<TemplateWarmupWorker>();
```

### Step 3: Handle Graceful Shutdown

The `BackgroundService` base class provides a `CancellationToken` that is triggered when the host is shutting down. Always:

1. Check `stoppingToken.IsCancellationRequested` in loops
2. Pass `cancellationToken` to all async operations
3. Log when shutdown is initiated

## Cron Expression Reference

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
| `0 */5 * * * ?` | Every 5 minutes | Stuck notification recovery |
| `0 0 * * * ?` | Every hour at :00 | Hourly cleanup |
| `0 0 2 * * ?` | Daily at 2am | Daily maintenance |
| `0 0 8 * * ?` | Daily at 8am | Morning batch |
| `0 0 8 * * MON-FRI ?` | Weekdays at 8am | Business hours only |
| `0 0 0 1 * ?` | First of month at midnight | Monthly reports |

### Testing Cron Expressions

Use https://crontab.guru/ (note: Quartz adds a seconds field, so prepend `0 ` to any standard cron expression).

## Step-by-Step: Testing Background Jobs Locally

### Step 1: Start the Service

```bash
cd services/notification-service/src/NotificationService.Api
dotnet run
```

### Step 2: Verify Jobs Are Registered

Check the application logs on startup:

```
[Information] Job 'notification-dispatch-job' scheduled with trigger 'notification-dispatch-trigger'
[Information] Job 'stuck-notification-recovery-job' scheduled with trigger 'stuck-notification-recovery-trigger'
```

### Step 3: Trigger a Job Manually (Optional)

In `Program.cs`, add a manual trigger endpoint for development:

```csharp
if (app.Environment.IsDevelopment())
{
    app.MapPost("/dev/trigger/dispatch", async (IScheduler scheduler) =>
    {
        await scheduler.TriggerJob(new JobKey("notification-dispatch-job", "notification"));
        return Results.Ok("Triggered");
    });
}
```

### Step 4: Monitor Job Execution

Check `logs/runtime-errors/runtime-error-<date>.txt` for job execution logs:

```
NotificationDispatchJob picked up 5 notification(s) to send.
```

## Misfire Policy

By default, Quartz uses `SimpleTrigger.MisfirePolicy.SmartPolicy`:

- If a trigger misses its fire time (e.g., service was down), it fires as soon as possible
- This is correct for notification dispatch — a missed 10-second slot should fire on the next tick

To customize, add to the trigger builder:

```csharp
var trigger = TriggerBuilder.Create()
    .WithIdentity("my-trigger")
    .WithCronSchedule("0 */5 * * * ?", x => x
        .WithMisfireHandlingInstructionFireAndProceed())
    .Build();
```

## Best Practices

1. **Always use `[DisallowConcurrentExecution]`** on jobs that must not overlap
2. **Resolve scoped services via `IServiceScopeFactory`** — never inject scoped services directly
3. **Make jobs idempotent** — assume they may run more than once
4. **Log structured information** — include job name, trigger name, correlation ID
5. **Handle failures gracefully** — log and continue, don't crash the process
6. **Batch large operations** — process in chunks of 100-1000 to avoid memory pressure
7. **Respect cancellation** — pass `context.CancellationToken` to all async operations
8. **Don't block** — use `await Task.Delay()`, never `Thread.Sleep()`

## Production Considerations

- **Monitoring**: Track job execution duration and success/failure rates
- **Alerting**: Alert on consecutive job failures
- **Concurrency**: Use `[DisallowConcurrentExecution]` for jobs that must not overlap
- **Timeouts**: Set reasonable timeouts via Quartz configuration
- **Graceful shutdown**: Allow jobs to complete during application shutdown (default behavior)
