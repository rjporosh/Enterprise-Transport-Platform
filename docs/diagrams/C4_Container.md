# C4 Model — Level 2: Container

What's inside the platform: services, datastores, and the observability
stack, and how they actually talk to each other today.

```mermaid
C4Container
    title Bus Ticketing Platform — Containers (built pieces only)

    Person(customer, "Customer")
    Person(admin, "Operations staff")

    Container(customerWeb, "Customer Web App", "Angular 19", "Search trips, select seats, hold a booking")
    Container(adminConsole, "Admin Console", "React 19 + TanStack Query", "View/cancel bookings")

    Container(bookingApi, "Booking Service API", ".NET 10 / ASP.NET Core Minimal APIs", "Trip search, seat holds, booking lifecycle")
    ContainerDb(postgres, "PostgreSQL", "Postgres 16", "Trips, seats, bookings, outbox — schema 'booking'")
    ContainerDb(redis, "Redis", "Redis 7", "Cache-aside for trip search results (30s TTL)")
    Container(rabbitmq, "RabbitMQ", "RabbitMQ 3.13", "booking.events topic exchange — booking.created / .confirmed / .cancelled")

    Container(jaeger, "Jaeger", "Distributed tracing", "Every request's trace, via OTLP")
    Container(prometheus, "Prometheus", "Metrics store", "Scrapes /metrics every 10s")
    Container(grafana, "Grafana", "Dashboards", "Visualizes Prometheus + Jaeger")
    Container(seq, "Seq", "Structured log store", "Serilog sink, queryable")

    Rel(customer, customerWeb, "Uses", "HTTPS")
    Rel(admin, adminConsole, "Uses", "HTTPS")

    Rel(customerWeb, bookingApi, "Search / book / cancel", "JSON/HTTPS")
    Rel(adminConsole, bookingApi, "List / view / cancel bookings", "JSON/HTTPS")

    Rel(bookingApi, postgres, "Reads/writes trips, bookings, outbox", "EF Core / Npgsql")
    Rel(bookingApi, redis, "Cache-aside reads/writes", "StackExchange.Redis")
    Rel(bookingApi, rabbitmq, "Publishes domain events (via outbox)", "AMQP")

    Rel(bookingApi, jaeger, "Exports traces", "OTLP/gRPC")
    Rel(prometheus, bookingApi, "Scrapes /metrics", "HTTP")
    Rel(grafana, prometheus, "Queries", "PromQL")
    Rel(grafana, jaeger, "Queries", "Jaeger API")
    Rel(bookingApi, seq, "Ships structured logs", "HTTP")
```

## Not built yet (from MASTER_SPEC.md)

API Gateway (YARP/Ocelot) in front of the services, Identity/Auth Service,
Route Service, Bus/Fleet Service, Payment Service, Notification Service —
these are folders under `services/` today with no implementation. The
Booking Service currently accepts requests directly (no gateway hop) and
validates its own JWTs, which is intentionally how it's designed to keep
working once a gateway is added in front of it (see the comment in
`Program.cs` about defense-in-depth JWT validation).

See [C4_Component.md](./C4_Component.md) for what's inside the Booking Service container.
