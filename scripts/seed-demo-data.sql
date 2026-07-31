-- Seed data for local development, Swagger/Scalar/Postman examples, and the
-- k6 load/stress tests. Run AFTER applying EF Core migrations:
--   dotnet ef database update --project services/booking-service/src/BookingService.Infrastructure \
--     --startup-project services/booking-service/src/BookingService.Api
--
-- Column names are PascalCase and case-sensitive (EF Core's default
-- convention, since no snake_case naming convention is configured) except
-- for the owned Money columns, which were explicitly mapped to snake_case —
-- that's not a typo, it matches BookingConfiguration.cs / TripConfiguration.cs.
--
-- Usage:
--   psql "postgresql://booking_svc:changeme@localhost:5432/booking_service" -f seed-demo-data.sql

BEGIN;

-- One route, one bus, reused by both trips below.
INSERT INTO booking."routes" ("Id", "OriginCity", "DestinationCity", "DistanceKm")
VALUES ('11111111-1111-1111-1111-111111111111', 'Dhaka', 'Chattogram', 264.00)
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO booking."buses" ("Id", "OperatorId", "PlateNumber", "BusType", "TotalSeats")
VALUES ('22222222-2222-2222-2222-222222222222', '99999999-9999-9999-9999-999999999999', 'DHK-1234', 'AC Sleeper', 36)
ON CONFLICT ("Id") DO NOTHING;

-- Trip #1: a normal-sized trip for browsing/search examples (36 seats, A1-F6 layout).
INSERT INTO booking."trips" ("Id", "RouteId", "BusId", "DepartureUtc", "ArrivalUtc", "Status", "base_price_amount", "base_price_currency")
VALUES (
  '3fa85f64-5717-4562-b3fc-2c963f66afa6',
  '11111111-1111-1111-1111-111111111111',
  '22222222-2222-2222-2222-222222222222',
  '2026-08-15 02:00:00+00',
  '2026-08-15 08:00:00+00',
  'Scheduled',
  1500.00,
  'BDT'
)
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO booking."trip_seats" ("Id", "TripId", "SeatNumber", "Deck", "Status")
SELECT gen_random_uuid(), '3fa85f64-5717-4562-b3fc-2c963f66afa6',
       chr(65 + (n / 6)) || ((n % 6) + 1)::text, 'Lower', 'Available'
FROM generate_series(0, 35) AS n
ON CONFLICT DO NOTHING;

-- Trip #2: deliberately tiny (4 seats) — use this trip id for the k6
-- create-booking-stress-test.js so contention is guaranteed.
INSERT INTO booking."trips" ("Id", "RouteId", "BusId", "DepartureUtc", "ArrivalUtc", "Status", "base_price_amount", "base_price_currency")
VALUES (
  '44444444-4444-4444-4444-444444444444',
  '11111111-1111-1111-1111-111111111111',
  '22222222-2222-2222-2222-222222222222',
  '2026-08-16 02:00:00+00',
  '2026-08-16 08:00:00+00',
  'Scheduled',
  1500.00,
  'BDT'
)
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO booking."trip_seats" ("Id", "TripId", "SeatNumber", "Deck", "Status")
VALUES
  (gen_random_uuid(), '44444444-4444-4444-4444-444444444444', 'A1', 'Lower', 'Available'),
  (gen_random_uuid(), '44444444-4444-4444-4444-444444444444', 'A2', 'Lower', 'Available'),
  (gen_random_uuid(), '44444444-4444-4444-4444-444444444444', 'A3', 'Lower', 'Available'),
  (gen_random_uuid(), '44444444-4444-4444-4444-444444444444', 'A4', 'Lower', 'Available')
ON CONFLICT DO NOTHING;

COMMIT;

-- Sanity check
SELECT "Id", "OriginCity", "DestinationCity" FROM booking."routes";
SELECT "Id", "DepartureUtc", "Status" FROM booking."trips";
