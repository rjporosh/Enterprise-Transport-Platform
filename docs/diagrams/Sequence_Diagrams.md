# Sequence diagrams

## Create booking (happy path + the concurrency conflict path)

```mermaid
sequenceDiagram
    actor Customer
    participant Web as Customer Web (Angular)
    participant API as Booking Service API
    participant App as CreateBookingHandler
    participant DB as PostgreSQL
    participant Cache as Redis
    participant MQ as RabbitMQ

    Customer->>Web: Selects seat A1, submits passenger details
    Web->>API: POST /api/v1/bookings {tripId, customerId, passengers}
    API->>App: CreateBookingCommand (via MediatR)
    App->>DB: SELECT Trip + Seats WHERE Id = tripId (tracked)
    DB-->>App: Trip { Seats: [A1=Available, ...] }
    App->>App: trip.HoldSeats(["A1"]) -- domain check, in memory
    App->>App: Booking.Create(...) -- raises BookingCreatedDomainEvent
    App->>DB: Add Booking, Add OutboxMessage (same DbContext)
    App->>DB: SaveChangesAsync() -- ONE transaction: seat hold + booking + outbox row
    alt No concurrent conflict
        DB-->>App: 1 row updated (xmin matched)
        App->>Cache: RemoveByPrefixAsync("trips:search:")
        App-->>API: BookingDto { status: PendingPayment, holdExpiresAtUtc: +10min }
        API-->>Web: 201 Created
        Web-->>Customer: "Seats held — pay within 10 minutes"
    else Concurrent conflict (another request booked A1 first)
        DB-->>App: DbUpdateConcurrencyException (xmin mismatch)
        App-->>API: SeatUnavailableException
        API-->>Web: 409 Conflict (ProblemDetails)
        Web-->>Customer: "That seat was just taken — pick another"
    end

    Note over MQ: Not shown: OutboxProcessor polls every 5s,<br/>relays the outbox row to RabbitMQ independently<br/>of the request/response above.
```

## Outbox relay (decoupled from the request above)

```mermaid
sequenceDiagram
    participant Processor as OutboxProcessor (BackgroundService)
    participant DB as PostgreSQL
    participant MQ as RabbitMQ

    loop every 5 seconds
        Processor->>DB: SELECT TOP 50 outbox_messages WHERE ProcessedOnUtc IS NULL ORDER BY OccurredOnUtc
        DB-->>Processor: [BookingCreatedDomainEvent, ...]
        loop each message
            Processor->>MQ: Publish(routingKey: "booking.created", payload)
            alt publish succeeds
                Processor->>DB: SET ProcessedOnUtc = now()
            else publish fails
                Processor->>DB: RetryCount += 1, Error = message
            end
        end
    end
```

This is what makes booking creation reliable even if the process crashes
between the HTTP response and the RabbitMQ publish — the event is already
durably stored in the same transaction as the booking itself, and the
processor will pick it up on the next poll after a restart.

## Trip search with cache-aside

```mermaid
sequenceDiagram
    actor Customer
    participant API as Booking Service API
    participant App as SearchTripsHandler
    participant Cache as Redis
    participant DB as PostgreSQL

    Customer->>API: GET /api/v1/trips/search?origin=Dhaka&destination=Chattogram&date=2026-08-15
    API->>App: SearchTripsQuery(page: 1, pageSize: 10)  Note: defaults applied if omitted
    App->>Cache: GET trips:search:dhaka:chattogram:2026-08-15:1:10
    alt Cache hit
        Cache-->>App: cached PagedResult<TripSearchResultDto>
    else Cache miss
        Cache-->>App: null
        App->>DB: SELECT ... JOIN routes JOIN buses WHERE ... (with per-seat availability subquery)
        DB-->>App: rows
        App->>Cache: SET (TTL 30s)
    end
    App-->>API: PagedResult
    API->>API: Add X-Pagination + X-Total-Count response headers
    API-->>Customer: 200 OK
```

See [../OBSERVABILITY_GUIDE.md](../OBSERVABILITY_GUIDE.md) to watch these
exact flows happen live in Jaeger/Grafana/Seq.
