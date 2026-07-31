# NBomber (.NET-native) load & stress tests

Same two scenarios as `../k6/`, written in C# with NBomber instead — useful
if your team wants performance tests in the same language/toolchain as the
service, runnable from `dotnet run` with no separate JS/Java runtime.

> **Compile note:** this project was written and reviewed by hand in an
> environment without a .NET SDK (see the root README's "How this was
> built" section) and NBomber's fluent API (`Http.CreateRequest`,
> `.WithJsonBody`, `Simulation.RampingInject`, etc.) has changed shape across
> major versions. Run `dotnet restore && dotnet build` first and check
> against the installed NBomber version's docs at https://nbomber.com/docs
> if anything doesn't match.

## Install

```bash
cd NBomber.LoadTests
dotnet restore
```

## Run — load scenario (search)

```bash
dotnet run -c Release -- --scenario search
```

Ramps 0 -> 20 requests/sec over 30s, holds at 20 req/s for 2 minutes, ramps
back down. Mirrors `../k6/search-trips-load-test.js`.

## Run — stress scenario (seat contention)

```bash
# 1. Seed a small-seat-count trip
psql "postgresql://booking_svc:changeme@localhost:5432/booking_service" \
  -f ../../../../scripts/seed-demo-data.sql

# 2. Get a dev JWT (see postman/README.md for the same trick used there)

# 3. Run
dotnet run -c Release -- --scenario stress \
  --trip-id 44444444-4444-4444-4444-444444444444 \
  --token <dev-jwt> \
  --seat A1
```

Fires 50 requests at the same seat within 1 second.

## Reading the results

NBomber writes an HTML + CSV + Markdown report to `reports/<scenario-name>/`
after each run.

- **Load scenario**: check the `search_trips_load` step's response time
  percentiles (p50/p95/p99) and the "OK/Fail" request counts — Fail should
  be ~0%.
- **Stress scenario**: NBomber doesn't have a built-in "exactly one success"
  assertion, so this one's a manual read — open the CSV, filter to
  `create_booking_seat_contention`, and count how many of the 50 rows have
  `status_code = 201`. It must be **exactly 1**. If it's 0, something else
  is wrong (bad trip id/token/seat). If it's more than 1, that's a real
  concurrency bug worth escalating immediately — cross-check it against
  `../k6/create-booking-stress-test.js`, which encodes the same assertion
  as an automatic threshold instead of a manual report read.

## Why two performance tools instead of one

k6 (`../k6/`) is the primary, CI-friendly option — its thresholds fail the
run automatically, no manual report-reading needed. NBomber and JMeter
(`../jmeter/`) are provided for teams that have already standardized on
.NET-native or JMeter-based tooling; all three test the same two behaviors
(search latency under load, seat-booking correctness under contention) so
picking one doesn't lose coverage.
