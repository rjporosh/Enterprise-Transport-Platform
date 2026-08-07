import http from 'k6/http';
import { check, sleep } from 'k6';

// Sustained send-throughput test: proves SendNotification stays responsive
// under load even though the actual channel send is deferred to
// NotificationDispatchJob (see SendNotificationHandler's remarks) — this is
// exactly what should make the write path fast regardless of SMTP/SMS/FCM
// latency, and this test is what actually demonstrates that design pays off.
export const options = {
  scenarios: {
    steady_send_load: {
      executor: 'constant-vus',
      vus: 25,
      duration: '2m',
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<300'], // the DB insert + outbox enqueue only -- no provider round-trip
    http_req_failed: ['rate<0.01'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5301';

export default function () {
  const payload = JSON.stringify({
    recipient: `loadtest-${__VU}-${__ITER}@example.com`,
    channel: 'Email',
    subject: 'Load test',
    body: 'This is a k6 load test notification.',
    priority: 'Normal',
    isTransactional: true,
  });

  const response = http.post(`${BASE_URL}/api/v1/notifications`, payload, {
    headers: { 'Content-Type': 'application/json' },
  });

  check(response, {
    'status is 201': (r) => r.status === 201,
  });

  sleep(0.2);
}
