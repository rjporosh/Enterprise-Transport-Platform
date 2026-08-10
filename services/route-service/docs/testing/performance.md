# Testing — Performance / Load

## Tools
- **k6** — scriptable load testing (`tests/load/k6/route-service-load.js`)
- **NBomber** — .NET-native load testing (`tests/load/nbomber/RouteServiceLoadTests.cs`)
- **JMeter** — GUI-based load test plan (`tests/load/jmeter/route-service-test-plan.jmx`)

## k6
```bash
k6 run tests/load/k6/route-service-load.js
```
- Ramp to 50 VUs over 30s, hold 1m
- Threshold: p95 < 500ms

## NBomber
```bash
dotnet run --project tests/load/nbomber/RouteServiceLoadTests.csproj
```
- Ramp constant 50 RPS for 30s

## JMeter
1. Open `tests/load/jmeter/route-service-test-plan.jmx` in Apache JMeter 5.6+
2. Update Thread Group ramp/threads as needed
3. Run against `http://localhost:5003`

## Performance Goals
- p95 latency < 500ms for list/search endpoints
- p95 latency < 300ms for point lookups
- Sustained throughput: 200 RPS on list endpoints

## Profiling
- Use `dotnet-trace` or `dotnet-counters` during NBomber runs
- Query logs are available in `logs/query-log-<date>.txt` when `Logging:EnableQueryLogging` is true
