# Testing — Performance Tests

## Tool

[k6](https://k6.io/) — scriptable load testing, runs in CI and locally.

## Scenarios

### 1. Read-heavy baseline

```javascript
import http from 'k6/http';
import { check } from 'k6';

export const options = {
  stages: [
    { duration: '30s', target: 50 },
    { duration: '1m', target: 50 },
    { duration: '10s', target: 0 }
  ]
};

export default function () {
  const res = http.get(`${__ENV.BASE_URL}/api/v1/buses?page=1&pageSize=20`, {
    headers: { Authorization: `Bearer ${__ENV.AUTH_TOKEN}` }
  });
  check(res, { 'status is 200': (r) => r.status === 200 });
}
```

### 2. Write burst (register bus)

```javascript
export const options = {
  scenarios: {
    register_bus: {
      executor: 'constant-arrival-rate',
      rate: 5,
      timeUnit: '1s',
      duration: '30s',
      maxVUs: 20
    }
  }
};

export default function () {
  const payload = JSON.stringify({
    operatorId: `${__ENV.OPERATOR_ID}`,
    plateNumber: `TEST-${Math.random().toString(36).slice(2, 10)}`,
    busType: 'AcSleeper',
    totalSeats: 40,
    depotId: `${__ENV.DEPOT_ID}`
  });

  const res = http.post(`${__ENV.BASE_URL}/api/v1/buses`, payload, {
    headers: {
      Authorization: `Bearer ${__ENV.AUTH_TOKEN}`,
      'Content-Type': 'application/json'
    }
  });
  check(res, { 'status is 200': (r) => r.status === 200 });
}
```

### 3. Cache hit ratio

Run `GET /api/v1/buses/{id}` repeatedly for the same bus. Expect >95% cache hit rate for Redis.

## Thresholds

| Metric | Target |
|---|---|
| p95 latency (read) | < 200ms |
| p95 latency (write) | < 500ms |
| Error rate | < 1% |
| Cache hit ratio | > 95% |

## Running

```bash
k6 run -e BASE_URL=http://localhost:5201 -e AUTH_TOKEN=<token> tests/load/k6/bus-service-load-test.js
```

## Stress Test

For stress testing, increase target VUs until error rate exceeds 5% or p95 latency exceeds 1s. Record the breaking point and the bottleneck (DB CPU, connection pool, RabbitMQ, Redis).
