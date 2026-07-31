# Observability guide: Seq, Grafana, Jaeger, Prometheus

Step-by-step for the actual question you'll have most often: **"a request
was slow / failed — why?"** Each tool answers a different slice of that
question; this doc shows which one to open and what to paste in.

Prerequisite: the stack is running (`docker compose up`, see
[RUNBOOK.md](./RUNBOOK.md)) and you've made at least one request so there's
data to look at.

## The mental model

| Tool | Answers | URL |
|---|---|---|
| **Seq** | "What did the app log for this request?" (structured logs, one line per event) | http://localhost:8081 |
| **Jaeger** | "Where did the time go, step by step, for this one request?" (distributed trace: HTTP -> DB query -> Redis call, each with start time + duration) | http://localhost:16686 |
| **Prometheus** | "What's the aggregate behavior over time?" (request rate, error rate, p95 latency, business counters) — raw query engine | http://localhost:9090 |
| **Grafana** | Same data as Prometheus + Jaeger, but as dashboards instead of raw queries | http://localhost:3000 (admin/admin) |

Every request carries a `CorrelationId` (see `CorrelationIdMiddleware.cs`,
returned as an `X-Correlation-Id` response header) — that's the thread that
ties a Seq log line to a Jaeger trace for the *same* request.

## 1. Generate something to look at

```bash
curl "http://localhost:8080/api/v1/trips/search?origin=Dhaka&destination=Chattogram&date=2026-08-15" -i
```

Copy the `X-Correlation-Id` value from the response headers — you'll use it below.

## 2. Find the request's logs in Seq

Open http://localhost:8081 -> the query bar at the top.

**Find this exact request:**
```
CorrelationId = 'paste-the-x-correlation-id-here'
```

**Find every slow request in the last hour** (the request-logging middleware
logs `Elapsed` in milliseconds for every HTTP call):
```
Elapsed > 400 and @Timestamp > Now() - 1h
```

**Find all errors for the booking service specifically:**
```
Service = 'booking-service' and @Level in ['Error', 'Fatal']
```

**Find the exact start time and duration of a specific query-like operation**
(e.g. every `SearchTrips` request, to see how request volume/timing changed
over a window you care about):
```
@MessageTemplate like '%HTTP GET /api/v1/trips/search%'
```
Click any row to expand it — the expanded view shows `@Timestamp` (exact
start time) and `Elapsed` (execution time in ms) as structured fields you
can also chart (Seq's "Chart" tab on a saved query, bucketed by time).

## 3. Find the same request's trace in Jaeger

Open http://localhost:16686.

1. **Service**: `booking-service`
2. **Operation**: `GET /api/v1/trips/search` (or leave as "all")
3. Optional: **Tags** field — `correlation.id=<the same X-Correlation-Id>` if
   you want the exact request (traces are also findable by time range alone
   for a dev environment with low traffic)
4. Click **Find Traces**

Click into a trace and you'll see a waterfall: the top-level HTTP span, a
child span for the Npgsql query (with the `db.statement` tag showing the
actual SQL text and its own start offset + duration), and — on a cache miss
— no Redis span; on a cache hit, a Redis `GET` span instead of the Postgres
one. **This is the tool for "which specific step took the time"** — Seq
tells you the total was slow, Jaeger tells you *which part*.

To specifically audit query timing: expand the Npgsql span, look at the
**Duration** field (exact execution time) and the **Start Time** relative
offset from the trace's root span (exact start, relative to when the
request began) — that pairing is what you need to decide whether a specific
query needs an index or a rewrite.

## 4. Aggregate view in Prometheus (raw PromQL)

Open http://localhost:9090 -> **Graph**.

**p95 latency by route, last 5 minutes:**
```promql
histogram_quantile(0.95, sum(rate(http_server_request_duration_seconds_bucket[5m])) by (le, http_route))
```

**Request rate by status code:**
```promql
sum(rate(http_server_request_duration_seconds_count[1m])) by (http_response_status_code)
```

**Seat-conflict rate (the metric that proves concurrency control is working under load):**
```promql
sum(rate(booking_seat_conflicts_total[1m])) * 60
```

**Bookings created per minute:**
```promql
sum(rate(bookings_created_total[1m])) * 60
```

## 5. Dashboards in Grafana

Open http://localhost:3000 (admin/admin) -> **Dashboards** -> **Bus
Ticketing** folder -> **Booking Service — Overview** (auto-provisioned from
`infrastructure/monitoring/grafana/dashboards/booking-service-overview.json`).

It has the four PromQL queries above as panels already, plus a GC heap size
panel. Adjust the time range (top-right) to the window you're investigating.

To add your own panel: **Edit** -> **Add panel** -> paste any PromQL query
from step 4 (or your own) -> **Apply**. To make it permanent, **Save
dashboard** — it edits the live Grafana state, not the JSON file on disk, so
export it back to
`infrastructure/monitoring/grafana/dashboards/booking-service-overview.json`
via **Dashboard settings -> JSON Model** if you want the change checked in.

## 6. Modifying the underlying query (when you find a slow one)

Once Jaeger shows you the exact SQL (`db.statement` tag) for a slow span:

1. Find the LINQ query that generates it — search
   `services/booking-service/src/BookingService.Application/Features/` for
   the handler matching the operation name.
2. Change the LINQ (add a filter, change a join, add `.AsSplitQuery()`,
   etc.).
3. Re-run the request, pull the trace again in Jaeger, compare the new
   `db.statement` and duration against the old one.
4. If it's a systemic pattern (not a one-off), add an index — see
   `docs/diagrams/ERD.md` for the indexes that already exist, and
   `TripConfiguration.cs`/`BookingConfiguration.cs` for where to add a new
   one (`builder.HasIndex(...)`), then generate a new EF Core migration.

## Quick reference: log fields you can filter on in Seq

| Field | Example query |
|---|---|
| `Service` | `Service = 'booking-service'` |
| `CorrelationId` | `CorrelationId = '<guid>'` |
| `Elapsed` (ms, on HTTP request-logging events) | `Elapsed > 1000` |
| `@Level` | `@Level = 'Warning'` |
| `RequestPath` | `RequestPath like '%/bookings%'` |
| `@Exception` (non-null on errors) | `@Exception is not null` |
