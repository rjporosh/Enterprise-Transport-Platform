# Troubleshooting

## Build Errors

### CS8122: Expression tree may not contain an 'is' pattern-matching operator

**Cause**: FluentAssertions converts lambdas to expression trees. Pattern matching (`is Type x`) is not supported in expression trees.

**Fix**: Use `.OfType<Type>().Single()` or `.Where(e => e is Type t && t.Property == value)`.

### DbUpdateConcurrencyException in InMemory tests

**Cause**: EF Core InMemory doesn't handle Postgres `xmin` rowversion properly.

**Fix**: Don't call `SaveChangesAsync` after mutating entities in tests when the entity has a concurrency token, or use a fresh DbContext instance.

## Runtime Errors

### Health check returns 503

**Cause**: PostgreSQL or RabbitMQ is unreachable.

**Fix**: Verify the connection strings and that the containers are running. Check `logs/runtime-errors/` for structured diagnostics.

### RabbitMQ consumer fails to start

**Cause**: RabbitMQ unreachable or credentials wrong.

**Fix**: The consumer fails gracefully — REST/gRPC endpoints remain functional. Verify `RabbitMq:*` configuration. Check the structured log for the exact connection error.

### Template not found

**Cause**: The template key/channel/locale combination doesn't exist or is inactive.

**Fix**: Verify the template exists and `IsActive = true`. Check the locale fallback: if `bn` template is missing, the system falls back to `en`.

### SMTP/SMS/Push delivery fails

**Cause**: Provider credentials missing or provider unreachable.

**Fix**: Verify `Smtp:*`, `Sms:*`, or `Push:*` configuration. Check `logs/runtime-errors/` for the exact error. The retry policy will retry transient failures up to `Retry:MaxAttempts` times.

## Database

### Migration fails with "relation does not exist"

**Cause**: The database schema is out of date.

**Fix**: Run `dotnet ef database update` or ensure the startup migration runs in Development mode.

### Switching database providers

**Cause**: Migrations are provider-specific.

**Fix**: Set `Database:Provider` in configuration, then regenerate migrations for the target provider.

## Performance

### High API latency

**Cause**: Check if the API is waiting for provider sends. The design keeps API latency bounded by DB insert time — if latency is high, check database query performance.

**Fix**: Check `logs/query-logs/` for slow queries. Ensure indexes exist on `Status`, `Recipient`, `CreatedAtUtc`.

### Notification dispatch delayed

**Cause**: `NotificationDispatchJob` runs every 10 seconds. Check Quartz scheduler logs.

**Fix**: Verify the Quartz trigger is firing. Check `Notification` rows for `Status = Pending` and `ScheduledForUtc <= now`.
