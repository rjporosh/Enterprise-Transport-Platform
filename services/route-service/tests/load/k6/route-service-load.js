import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  stages: [
    { duration: '30s', target: 50 },
    { duration: '1m', target: 50 },
    { duration: '10s', target: 0 },
  ],
  thresholds: {
    http_req_duration: ['p(95)<500'],
  },
};

export default function () {
  const res = http.get('http://localhost:5003/api/v1/routes?page=1&pageSize=20');
  check(res, {
    'status is 200': (r) => r.status === 200,
    'response has items': (r) => JSON.parse(r.body as string).items.length > 0,
  });
  sleep(1);
}
