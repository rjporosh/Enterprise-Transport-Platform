# Cron Expressions

## Quartz Cron Format

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

## Common Patterns

| Expression | Description |
|---|---|
| `0 * * * * ?` | Every hour at minute 0 |
| `*/10 * * * * ?` | Every 10 seconds |
| `0 */5 * * * ?` | Every 5 minutes |
| `0 0 8 * * ?` | Daily at 8:00 AM |
| `0 0 8 * * MON-FRI ?` | Weekdays at 8:00 AM |
| `0 0 0 1 * ?` | First day of each month at midnight |

## Testing Cron Expressions

Use [crontab.guru](https://crontab.guru/) for validation, noting Quartz adds a seconds field.

## Misfire Policy

By default, Quartz uses `SimpleTrigger.MisfirePolicy.SmartPolicy` — if a trigger misses its fire time, it fires as soon as possible. For notification dispatch this is correct: a missed 10-second slot should fire on the next scheduler tick, not be skipped.
