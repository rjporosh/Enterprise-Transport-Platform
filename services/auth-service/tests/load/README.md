# Load & stress tests — k6, JMeter, NBomber

Three tools, same handful of scenarios (login load, register race/spike),
so pick whichever fits your team instead of standardizing on all three.

| Tool | Folder | Best for |
|---|---|---|
| **k6** | `k6/` | Fast to run locally/CI, scripts-as-code, great summary output. Default recommendation. |
| **JMeter** | `jmeter/` | Teams with existing JMeter plans/CI steps, GUI-driven test design, distributed load generation via JMeter's master/worker mode. |
| **NBomber** | `nbomber/` | .NET-native — write scenarios in C# alongside the codebase, reuse the app's own HttpClient/DI patterns. |

## What each scenario proves

| Scenario | What it proves |
|---|---|
| Login load (steady ~25 concurrent users, 3 min) | Sign-in latency (PBKDF2 verify + JWT issue + refresh-token insert) stays acceptable under realistic traffic — k6/JMeter/NBomber all assert **p95 < 400ms** and **&lt;1% errors**. |
| Register race (50 concurrent requests, same email) | The email-uniqueness check is actually correct under real concurrency, not just in a single-threaded unit test — **exactly one** request may succeed; every other one must be `409 Conflict`, never a duplicate row. |
| Register spike + rate limiting | The `auth-write` rate limiter (10 req/min/IP — see Program.cs) actually returns `429` once exceeded, rather than silently degrading. |

## Running each

### k6
```bash
cd k6
k6 run -e BASE_URL=http://localhost:8081 login-load-test.js
k6 run -e BASE_URL=http://localhost:8081 register-stress-test.js
# No local install: docker run --rm -i grafana/k6 run - < login-load-test.js
```

### JMeter
```bash
cd jmeter
./generate-test-users.sh 50            # writes test-users.csv for the Login Load thread group
jmeter -n -t auth-service-load-test.jmx -Jbase_url=http://localhost:8081 -l results.jtl
# Or open auth-service-load-test.jmx in the JMeter GUI to inspect/tune it first.
```

### NBomber
```bash
cd nbomber
BASE_URL=http://localhost:8081 dotnet run -c Release
# HTML + CSV report written to nbomber/reports/
```

## Reading the results

- **Login load**: fails its threshold if p95 latency exceeds 400ms or the
  error rate exceeds 1%. If it fails, check whether PBKDF2's 100k
  iterations (see `PasswordHasher.cs`) is the bottleneck before anything
  else — it is deliberately slow, by design, and CPU-bound password
  verification is usually the first thing that saturates under login load.
- **Register race**: fails if more than one concurrent request for the
  same email returns `200`. `RegisterHandler` handles this two ways: an
  `AnyAsync` pre-check for the common case, plus a `catch` around
  `SaveChangesAsync` that translates the unique-index violation from a
  genuine DB-level race into the same `409 Conflict` — see the comment in
  `RegisterHandler.cs`. This load test is what actually proves that path
  works, since a single-threaded unit test can'"'"'t create the race.

## Prerequisites

All three need Auth Service (and its Postgres/Redis/RabbitMQ dependencies)
running first — `docker-compose up` from `infrastructure/docker`, or run
`dotnet run` in `src/AuthService.Api` against locally running dependencies.
None of these could actually be executed in the sandbox that generated this
repo (no Docker, no network) — treat all three as the intended tests; run
them for real locally or in CI to get numbers.
