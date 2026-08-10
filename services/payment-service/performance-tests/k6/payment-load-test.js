import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '30s', target: 20 },
    { duration: '1m', target: 20 },
    { duration: '10s', target: 0 },
  ],
  thresholds: {
    'http_req_failed': ['rate<0.01'],
    'http_req_duration': ['p(95)<500'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5003';
const AUTH_TOKEN = __ENV.AUTH_TOKEN || '';

export function setup() {
  return { token: AUTH_TOKEN };
}

export default function (data) {
  const headers = {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${data.token}`,
  };

  const payload = JSON.stringify({
    TenantId: '11111111-1111-1111-1111-111111111111',
    CustomerId: '44444444-4444-4444-4444-444444444444',
    OrderReference: `LOAD-${Date.now()}`,
    PaymentMethod: 'Card',
    Amount: 100.00,
    Currency: 'USD',
    IdempotencyKey: `load-${Date.now()}`,
    TtlMinutes: 30,
  });

  const res = http.post(`${BASE_URL}/api/v1/payments`, payload, { headers });

  check(res, {
    'status is 201': (r) => r.status === 201,
    'has PaymentId': (r) => r.json('PaymentId') !== undefined,
  });

  sleep(1);
}
