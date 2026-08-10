# C4 Component Diagram

```mermaid
graph LR
    subgraph "BusService.Api"
        Endpoints["BusEndpoints<br/>(Minimal API Mappers)"]
        Middleware["Middleware<br/>(Correlation, Exception, RateLimit, Idempotency)"]
        GrpcService["BusGrpcService"]
        Diagnostics["RuntimeErrorLogWriter"]
    end

    subgraph "BusService.Application"
        Commands["Commands<br/>(RegisterBus, UpdateBus, ChangeStatus, ...)"]
        Queries["Queries<br/>(GetBus, GetBuses, GetDepots)"]
        Validators["FluentValidation Validators"]
        Behaviors["Pipeline Behaviors<br/>(Validation, Logging)"]
        DTOs["DTOs<br/>(BusDto, DepotDto, PagedResult)"]
    end

    subgraph "BusService.Infrastructure"
        DbContext["BusDbContext"]
        Repositories["Repositories<br/>(Generic + Specific)"]
        CacheSvc["RedisCacheService"]
        EventPublisher["OutboxEventPublisher"]
        OutboxProcessor["OutboxProcessor<br/>(Background Service)"]
        AuditLogger["AuditLogger"]
        QueryInterceptor["QueryLoggingInterceptor"]
        Metrics["BusMetrics<br/>(OpenTelemetry)"]
        Localization["JsonLocalizationService"]
    end

    subgraph "BusService.Domain"
        Bus["Bus<br/>(Aggregate Root)"]
        Depot["Depot<br/>(Entity)"]
        BusEvents["Domain Events"]
        BusExceptions["Domain Exceptions"]
        BusEnums["BusStatus, BusType"]
    end

    Endpoints --> Commands
    Endpoints --> Queries
    Commands --> Behaviors
    Queries --> Behaviors
    Behaviors --> Validators
    Commands --> Repositories
    Queries --> Repositories
    Repositories --> DbContext
    DbContext --> Bus
    DbContext --> Depot
    Commands --> BusEvents
    Bus --> BusEvents
    Commands --> AuditLogger
    Commands --> EventPublisher
    EventPublisher --> DbContext
    OutboxProcessor --> RabbitMQ
    QueryInterceptor --> DbContext
```

## Description

- **Endpoints**: Map HTTP/gRPC routes, enforce auth/rate-limiting, delegate to MediatR.
- **Middleware**: Cross-cutting concerns — correlation ID, exception shaping, request context, idempotency, rate limiting.
- **Pipeline Behaviors**: Validation (FluentValidation) and logging wrap every command/query.
- **Repositories**: Implement `IBusDbContext`, encapsulate EF Core queries.
- **Outbox**: `OutboxEventPublisher` persists domain events within the same transaction; `OutboxProcessor` (hosted service) dispatches to RabbitMQ.
- **AuditLogger**: Records every write operation with user, changes, IP, and correlation ID.
- **QueryLoggingInterceptor**: Optional EF Core interceptor for diagnostic SQL logging.
