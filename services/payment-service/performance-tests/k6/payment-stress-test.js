import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  scenarios: [
    {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '30s', target: 50 },
        { duration: '1m', target: 50 },
        { duration: '30s', target: 0 },
      ],
    },
  ],
  thresholds: {
    'http_req_failed': ['rate<0.02'],
    'http_req_duration': ['p(95)<800'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5003';
const AUTH_TOKEN = __ENV.AUTH_TOKEN || '';

export default function () {
  const headers = {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${AUTH_TOKEN}`,
  };

  for (let i = 0; i < 10; i++) {
    const payload = JSON.stringify({
      TenantId: '11111111-1111-1111-1111-111111111111',
      CustomerId: '44444444-4444-4444-4444-444444444444',
      OrderReference: `STRESS-${Date.now()}-${i}`,
      PaymentMethod: 'Card',
      Amount: 50 + i,
      Currency: 'USD',
      IdempotencyKey: `stress-${Date.now()}-${i}`,
      TtlMinutes: 30,
    });

    const res = http.post(`${BASE_URL}/api/v1/payments`, payload, { headers });

    check(res, {
      'status is 201 or 409': (r) => r.status === 201 || r.status === 409,
    });

    sleep(0.1);
  }
}
