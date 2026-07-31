# C4 Model — Level 3: Component (Booking Service)

Inside the Booking Service container: the four Clean Architecture layers and
their real dependency direction (arrows point toward the Domain — nothing in
Domain knows about EF Core, Redis, or ASP.NET).

```mermaid
C4Component
    title Booking Service — Components

    Container_Boundary(api, "BookingService.Api") {
        Component(endpoints, "Endpoints", "Minimal API", "TripsEndpoints, BookingsEndpoints — HTTP in/out, pagination headers")
        Component(middleware, "Middleware", "ASP.NET Core", "ExceptionHandlingMiddleware (-> ProblemDetails), CorrelationIdMiddleware")
    }

    Container_Boundary(application, "BookingService.Application") {
        Component(handlers, "CQRS Handlers", "MediatR", "SearchTripsHandler, CreateBookingHandler, CancelBookingHandler, GetBookingByIdHandler")
        Component(behaviors, "Pipeline Behaviors", "MediatR", "ValidationBehavior (FluentValidation), LoggingBehavior")
        Component(ports, "Ports (interfaces)", "C#", "IBookingDbContext, ICacheService, IEventPublisher, IBookingMetrics, IDateTimeProvider")
    }

    Container_Boundary(domain, "BookingService.Domain") {
        Component(aggregates, "Aggregates", "C#", "Trip (seat inventory + holds), Booking (lifecycle + total)")
        Component(events, "Domain Events", "C#", "BookingCreated/Confirmed/Cancelled")
    }

    Container_Boundary(infra, "BookingService.Infrastructure") {
        Component(efcore, "Persistence", "EF Core + Npgsql", "BookingDbContext, entity configurations, outbox table")
        Component(cache, "Caching", "StackExchange.Redis", "RedisCacheService — fails open on Redis errors")
        Component(messaging, "Messaging", "RabbitMQ.Client", "OutboxProcessor (poll+relay), RabbitMqPublisher")
        Component(otel, "Observability", "OpenTelemetry", "BookingMetrics (custom counters/histograms)")
    }

    Rel(endpoints, handlers, "Sends commands/queries via", "MediatR ISender")
    Rel(handlers, behaviors, "Wrapped by")
    Rel(handlers, ports, "Depends on (not on Infrastructure directly)")
    Rel(handlers, aggregates, "Loads, mutates, saves")
    Rel(aggregates, events, "Raises")
    Rel(efcore, ports, "Implements IBookingDbContext")
    Rel(cache, ports, "Implements ICacheService")
    Rel(messaging, ports, "Implements IEventPublisher (via outbox)")
    Rel(otel, ports, "Implements IBookingMetrics")
```

## The rule this diagram is actually enforcing

`BookingService.Domain` has exactly one dependency: `MediatR.Contracts`, for
the `INotification` marker interface domain events implement (see the
comment in `Domain/Common/DomainEvent.cs` for why that's a deliberate,
narrow exception). Everything else — EF Core, Redis, RabbitMQ, ASP.NET —
lives in `Infrastructure` or `Api`, reached only through interfaces defined
in `Application`. That's what makes `CreateBookingHandlerTests.cs` able to
run against an in-memory EF provider and a fake cache with zero real
infrastructure spun up.

See [C4_Code.md](./C4_Code.md) for the class-level shape of the Booking/Trip aggregates.
