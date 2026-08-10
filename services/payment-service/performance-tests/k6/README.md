# Payment Service - Performance Tests

## Prerequisites

- k6 installed: https://k6.io/docs/getting-started/installation/
- Payment Service running at `http://localhost:5003`
- Valid JWT token (set `AUTH_TOKEN` env var)

## How to Run

### Load Test

```bash
cd services/payment-service/performance-tests/k6
AUTH_TOKEN="your-jwt-token" k6 run payment-load-test.js
```

### Stress Test

```bash
cd services/payment-service/performance-tests/k6
AUTH_TOKEN="your-jwt-token" k6 run payment-stress-test.js
```

## Metrics

- RPS (requests per second)
- P95, P99 latency
- Error rate
- Failed requests

## Thresholds

- `http_req_failed` < 1%
- `http_req_duration` p(95) < 500ms
