# Programmer's Guide — Repository Pattern

## Overview

All data access goes through repository interfaces defined in `Application.Common.Interfaces` and implemented in `Infrastructure`.

## IBusDbContext

The primary abstraction for data access:

```csharp
public interface IBusDbContext
{
    DbSet<Bus> Buses { get; }
    DbSet<Depot> Depots { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

## Generic Repository

A generic repository reduces boilerplate for common CRUD:

```csharp
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Delete(T entity);
}
```

## Usage in Handlers

```csharp
public class GetBusHandler : IRequestHandler<GetBusQuery, BusDto>
{
    private readonly IBusDbContext _dbContext;
    private readonly ICacheService _cache;
    private readonly ILogger<GetBusHandler> _logger;

    public GetBusHandler(IBusDbContext dbContext, ICacheService cache, ILogger<GetBusHandler> logger)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<BusDto> Handle(GetBusQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"bus:{request.BusId}";
        if (await _cache.GetAsync<BusDto>(cacheKey, cancellationToken) is { } cached)
            return cached;

        var bus = await _dbContext.Buses
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BusId, cancellationToken);

        if (bus is null)
            throw new BusNotFoundException(request.BusId);

        var dto = new BusDto(/* map */);
        await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(5), cancellationToken);
        return dto;
    }
}
```

## Query Optimization

- Use `AsNoTracking()` for read-only queries.
- Use `FindAsync()` for PK lookups — it checks the change tracker first.
- Use explicit `Include()` only when needed; avoid eager-loading navigation properties that aren't used.
- The `QueryLoggingInterceptor` (opt-in via `Logging:EnableQueryLogging`) logs every SQL statement with duration for tuning.
