# Payment Service - NBomber Performance Tests

## Prerequisites

- .NET 10 SDK
- Payment Service running at `http://localhost:5003`

## How to Run

```bash
dotnet run --project services/payment-service/performance-tests/nbomber/PaymentLoadTests
```

Or with arguments:

```bash
dotnet run --project services/payment-service/performance-tests/nbomber/PaymentLoadTests -- http://localhost:5003 your-jwt-token
```

## Metrics Captured

- RPS (requests per second)
- Latency (min, mean, max, P50, P75, P90, P95, P99)
- OK/Fail counts
- Data transfer

## Report

Reports are generated in `test-results/nbomber/`.
