# Notification Service Load Tests

Performance and stress tests for the Notification Service API.

## Structure

```
tests/load-test/
├── nbomber/          # .NET-native load/stress tests (NBomber)
├── k6/               # Scriptable HTTP load/stress tests (k6)
└── jmeter/           # Enterprise performance test plans (JMeter)
```

## Prerequisites

- The Notification Service running (default: `http://localhost:5301`)
- For k6: [k6](https://k6.io/docs/getting-started/installation/) installed
- For NBomber: .NET 10 SDK
- For JMeter: [Apache JMeter](https://jmeter.apache.org/download_jmeter.cgi) 5.6+

Set `BASE_URL` environment variable to override the target:
```bash
export BASE_URL=http://localhost:5301
```

## k6

### Load test (sustained expected traffic)
```bash
k6 run tests/load-test/k6/send-notification-load-test.js
```

### Stress test (ramp past breaking point)
```bash
k6 run tests/load-test/k6/send-notification-stress-test.js
```

**Thresholds** (load test):
- `http_req_duration` p95 < 300ms
- `http_req_failed` rate < 1%

## NBomber

### Load + Stress + Query scenarios
```bash
cd tests/load-test/nbomber/NotificationService.LoadTests.Nbomber
dotnet run -c Release
```

Reports are written to `tests/load-test/nbomber/NotificationService.LoadTests.Nbomber/reports/`.

**Scenarios**:
1. `send_notification_load` — 20 RPS ramp-up, 2 min hold, 30s ramp-down
2. `send_notification_stress` — 50→100→200 RPS ramp
3. `get_notifications_load` — 30 RPS read load for 2 min

## JMeter

### Run the API performance test
```bash
jmeter -n \
  -t tests/load-test/jmeter/notification-api-performance.jmx \
  -l results.jtl \
  -Jbase_url=http://localhost:5301
```

View results:
```bash
jmeter -g results.jtl -o jmeter-report
```

**Test plan**: 10 threads, 30s ramp-up, 100 iterations each, POST `/api/v1/notifications`.

## Interpreting Results

| Metric | Load Target | Notes |
|---|---|---|
| RPS (writes) | 20 | Sustained, no errors |
| P95 latency | < 300ms | Write path (DB insert only) |
| P95 latency (reads) | < 200ms | Paged listing |
| Error rate | < 1% | Excludes expected 400/409 validation errors |
| Stress breaking point | Recorded | Where error rate exceeds 5% |

Never run heavy load/stress tests against production.
