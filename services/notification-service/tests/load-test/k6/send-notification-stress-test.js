import http from 'k6/http';
import { check, sleep } from 'k6';

// Stress test: ramp up send throughput past the load-test ceiling
// to find the service's breaking point. Run after the load test passes.
export const options = {
  scenarios: {
    stress_ramp: {
      executor: 'ramping-vus',
      startVUs: 10,
      stages: [
        { duration: '1m', target: 50 },
        { duration: '1m', target: 100 },
        { duration: '1m', target: 200 },
      ],
      gracefulRampDown: '30s',
    },
  },
  thresholds: {
    http_req_failed: ['rate<0.05'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5301';

export default function () {
  const payload = JSON.stringify({
    recipient: `stress-${__VU}-${__ITER}@example.com`,
    channel: 'Email',
    subject: 'Stress test',
    body: 'This is a k6 stress test notification.',
    priority: 'Normal',
    isTransactional: true,
  });

  const response = http.post(`${BASE_URL}/api/v1/notifications`, payload, {
    headers: { 'Content-Type': 'application/json' },
  });

  check(response, {
    'status is 201 or 429': (r) => r.status === 201 || r.status === 429,
  });

  sleep(0.1);
}
