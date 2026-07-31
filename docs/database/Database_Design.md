# Database design

The real, implemented schema — see [diagrams/ERD.md](../diagrams/ERD.md) for
the visual version. This page covers the reasoning `ERD.md` doesn't have
room for.

## Schema: `booking`

All tables for the Booking Service live under the Postgres `booking` schema
(`modelBuilder.HasDefaultSchema("booking")` in `BookingDbContext.cs`) rather
than `public` — when more services share this Postgres instance in local
dev, each gets its own schema instead of colliding table names.

## Table-by-table

### `routes`, `buses`

Read-only replicas of data owned by (eventually) the Route Service and Bus
Service — Booking Service doesn't own route/bus master data, it just needs
it fast, locally, without a synchronous cross-service call on the trip
search hot path. In the target architecture these get kept in sync via
integration events; today they're seeded directly (`scripts/seed-demo-data.sql`)
since those services don't exist yet.

### `trips`

One row per scheduled departure. `base_price_amount`/`base_price_currency`
are the two columns of the owned `Money` value object — explicitly
snake_case-mapped (`TripConfiguration.cs`), unlike every other column in
this database, which keeps EF Core's default PascalCase naming. `xmin` is
Postgres' native row-versioning system column, mapped as the optimistic
concurrency token (see [ERD.md](../diagrams/ERD.md) for why, and
[C4_Code.md](../diagrams/C4_Code.md) for how it prevents double-booking).

### `trip_seats`

One row per physical seat per trip. `(TripId, SeatNumber)` has a unique
index — the database itself refuses to let you insert a duplicate seat for
a trip, on top of the application-level checks.

### `bookings`

One row per reservation. `total_amount`/`currency` are the owned `Money`
columns (same snake_case pattern as `trips`). `TripId` is stored but has
**no foreign key constraint** — see the ERD doc for why that's deliberate.

### `booking_seats`

One row per seat within a booking (a booking can have multiple passengers/seats).

### `outbox_messages`

The transactional outbox. `Payload` is `jsonb` (queryable, not just an opaque
blob — you can `SELECT Payload->>'BookingId' FROM booking.outbox_messages`
if you need to debug a specific event). Indexed on
`(ProcessedOnUtc, OccurredOnUtc)` to match exactly how `OutboxProcessor`
polls it.

## Indexing decisions

| Index | Table | Why |
|---|---|---|
| `(RouteId, DepartureUtc)` | `trips` | Exact filter shape of `SearchTrips` |
| `(TripId, SeatNumber)` unique | `trip_seats` | Prevents duplicate seat rows at the DB level |
| `CustomerId` | `bookings` | "My bookings" lookups |
| `TripId` | `bookings` | "Bookings for this trip" (admin console, cancellation flows) |
| `(ProcessedOnUtc, OccurredOnUtc)` | `outbox_messages` | Matches `OutboxProcessor`'s poll query exactly |

If you add a new query pattern that doesn't match one of these, don't guess
— use Jaeger to see the actual generated SQL for the slow query (see
[../OBSERVABILITY_GUIDE.md](../OBSERVABILITY_GUIDE.md) section 3), run
`EXPLAIN ANALYZE` against it locally, and only then decide whether a new
index is the fix.

## Generating a migration for a schema change

```bash
cd services/booking-service
dotnet ef migrations add <DescriptiveName> \
  --project src/BookingService.Infrastructure \
  --startup-project src/BookingService.Api
dotnet ef database update \
  --project src/BookingService.Infrastructure \
  --startup-project src/BookingService.Api
```

Always review the generated migration file before applying it — EF Core is
usually right about what SQL to generate, but "usually" is worth a 30-second
read, especially for anything involving a column rename (EF Core defaults to
drop+recreate, which loses data, unless you hand-edit the migration to use
`RenameColumn`).
