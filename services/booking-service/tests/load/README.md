# Load & stress tests (k6)

Two scripts, two different purposes:

| Script | Type | What it proves |
|---|---|---|
| `search-trips-load-test.js` | **Load test** — steady, realistic traffic | Search latency stays acceptable under normal load, and the Redis cache-aside layer measurably helps (compare `search_duration_cold` vs `search_duration_warm` in the summary) |
| `create-booking-stress-test.js` | **Stress test** — adversarial spike, 50 VUs racing for one seat | The optimistic-concurrency seat-hold logic is actually correct under real concurrent contention, not just in a single-threaded unit test |

## Install k6

```bash
brew install k6            # macOS
choco install k6            # Windows
docker run --rm -i grafana/k6 run - < search-trips-load-test.js   # no local install
```

## Run

```bash
# 1. Load test (no auth required — search is public)
k6 run -e BASE_URL=http://localhost:8080 search-trips-load-test.js

# 2. Seed a trip with few seats to maximize contention, then stress test it
psql -h localhost -U booking_svc -d booking_service -f ../../../../scripts/seed-demo-data.sql
k6 run \
  -e BASE_URL=http://localhost:8080 \
  -e TRIP_ID=<paste-a-seeded-trip-id> \
  -e ACCESS_TOKEN=<a-valid-dev-jwt> \
  -e SEAT_NUMBER=A1 \
  create-booking-stress-test.js
```

## Reading the results

- `search-trips-load-test.js`: fails its threshold if p95 latency exceeds
  400ms overall or 150ms for "warm" (post-cache) requests, or if the error
  rate exceeds 1%.
- `create-booking-stress-test.js`: fails its threshold if **more than one**
  of the 50 concurrent requests for the same seat returns 201 Created. That
  would mean two customers were sold the same seat — a correctness bug, not
  a performance issue, which is exactly why this is written as a k6 script
  and not left to eyeballing logs.

## Not included yet

An NBomber (.NET-native) equivalent and a JMeter plan are referenced in
`MASTER_SPEC.md`'s tooling list but not built here — k6 covers both test
types adequately for this slice; add the others if your team standardizes
on them instead.
