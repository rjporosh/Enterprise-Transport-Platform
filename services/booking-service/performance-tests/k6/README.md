# k6 load & stress tests

The primary, CI-friendly performance test suite — see
`../jmeter/` and `../nbomber/` for the same two scenarios in other tools.

| Script | Type | What it proves |
|---|---|---|
| `search-trips-load-test.js` | **Load test** — steady, realistic traffic | Search latency stays acceptable under normal load, and the Redis cache-aside layer measurably helps |
| `create-booking-stress-test.js` | **Stress test** — adversarial spike, 50 VUs racing for one seat | The optimistic-concurrency seat-hold logic is correct under real concurrent contention, not just in a single-threaded unit test |

## Install k6

```bash
brew install k6                # macOS
choco install k6                # Windows
docker run --rm -i grafana/k6 run - < search-trips-load-test.js   # no local install
```

## Run

```bash
# 1. Load test (no auth required — search is public)
k6 run -e BASE_URL=http://localhost:8080 search-trips-load-test.js

# 2. Seed a trip with few seats to maximize contention, then stress test it
psql "postgresql://booking_svc:changeme@localhost:5432/booking_service" \
  -f ../../../../scripts/seed-demo-data.sql

k6 run \
  -e BASE_URL=http://localhost:8080 \
  -e TRIP_ID=44444444-4444-4444-4444-444444444444 \
  -e ACCESS_TOKEN=<a-valid-dev-jwt> \
  -e SEAT_NUMBER=A1 \
  create-booking-stress-test.js
```

Get a dev JWT the same way the Postman collection does — see
`../../../../postman/README.md`.

## Reading the results

- `search-trips-load-test.js`: fails its threshold if p95 latency exceeds
  400ms overall or 150ms for "warm" (post-cache) requests, or if the error
  rate exceeds 1%.
- `create-booking-stress-test.js`: fails its threshold if **more than one**
  of the 50 concurrent requests for the same seat returns 201 Created. That
  would mean two customers were sold the same seat — a correctness bug.

For a step-by-step on cross-checking a slow/failing run against Jaeger
traces, Prometheus queries, and Seq logs (with the exact queries to paste
in), see `../../../../docs/OBSERVABILITY_GUIDE.md`.
