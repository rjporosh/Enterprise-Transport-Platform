# Repository

## Pattern

The Application layer defines port interfaces; Infrastructure implements them.

### INotificationDbContext

```csharp
public interface INotificationDbContext
{
    DbSet<Notification> Notifications { get; }
    DbSet<NotificationTemplate> NotificationTemplates { get; }
    DbSet<RecipientPreference> RecipientPreferences { get; }
    DbSet<NotificationLog> NotificationLogs { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

Handlers receive `INotificationDbContext` via constructor injection. The implementation (`NotificationDbContext`) is registered in `Infrastructure/DependencyInjection.cs`.

## Usage

```csharp
public async Task<Result<PagedResult<NotificationDto>>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
{
    var query = _dbContext.Notifications.AsNoTracking().Where(n => !n.IsDeleted);

    if (request.Channel is not null)
        query = query.Where(n => n.Channel == request.Channel);

    // ... more filters

    var totalCount = await query.CountAsync(cancellationToken);
    var items = await query
        .OrderByDescending(n => n.CreatedAtUtc)
        .Skip((request.Page - 1) * request.PageSize)
        .Take(request.PageSize)
        .Select(n => new NotificationDto(...))
        .ToListAsync(cancellationToken);

    return Result<PagedResult<NotificationDto>>.Success(
        new PagedResult<NotificationDto>(items, totalCount, request.Page, request.PageSize));
}
```

## Soft Delete Queries

Global query filters automatically exclude soft-deleted rows. To include deleted rows:
```csharp
var all = await _dbContext.Notifications
    .IgnoreQueryFilters()
    .ToListAsync(cancellationToken);
```

## Optimistic Concurrency

`Notification` uses Postgres `xmin` rowversion. `NotificationTemplate` uses `bytea` rowversion. If a concurrency conflict occurs, EF Core throws `DbUpdateConcurrencyException` which is caught by the centralized exception handler.
