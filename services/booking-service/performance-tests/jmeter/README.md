# JMeter test plan

`booking-service-load-test.jmx` — a Thread Group of 20 users ramping up over
30s, each running 10 loops of `GET /api/v1/trips/search`, asserting `200 OK`
and a response time under 400ms per request. Same intent as
`../k6/search-trips-load-test.js`, for teams standardized on JMeter/GUI-based
tooling instead of scripts.

## Install JMeter

Download from https://jmeter.apache.org/download_jmeter.cgi (requires a JDK).
On macOS: `brew install jmeter`.

## Run — GUI mode (for building/debugging the plan)

```bash
jmeter -t booking-service-load-test.jmx
```

GUI mode is for authoring only — **never run a real load test in GUI mode**,
it skews your own results (JMeter's UI rendering competes with the load
generation for CPU).

## Run — CLI mode (for the actual test run)

```bash
jmeter -n -t booking-service-load-test.jmx \
  -JHOST=localhost -JPORT=8080 \
  -l results.jtl \
  -e -o report/
```

- `-n` — non-GUI mode (this is the one that gives you real numbers)
- `-JHOST` / `-JPORT` — override the target host/port (defaults to `localhost:8080`)
- `-l results.jtl` — raw results file
- `-e -o report/` — generate an HTML dashboard report into `report/` after the run

Open `report/index.html` when it finishes for graphs, response time
percentiles, and error breakdowns.

## Reading the results

- **Summary Report** listener (built into the plan) gives you min/max/avg/p90
  response times and error % live if run in GUI mode against a small sample.
- In CLI mode, check `report/index.html` -> "APDEX" and "Response Times Over
  Time" panels. If the Duration Assertion (400ms) is failing for a meaningful
  percentage of requests, that's your signal to look at Grafana/Jaeger for
  the same time window — see `../../../../docs/OBSERVABILITY_GUIDE.md`.

## Extending this plan

To add a stress-test-style scenario (many users hitting the same seat, like
`../k6/create-booking-stress-test.js` does), duplicate the Thread Group,
point the HTTP sampler at `POST /api/v1/bookings` with a fixed `seatNumber`,
add an `Authorization: Bearer ${TOKEN}` header (User Defined Variable +
HTTP Header Manager), and assert on response code being *either* 201 or 409
via a JSR223 Assertion — JMeter's built-in Response Assertion can't express
"one of these codes" as cleanly as k6/NBomber can, which is exactly why the
seat-contention correctness test lives in those two instead of here.
