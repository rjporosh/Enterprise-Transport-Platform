// k6 load test: steady, realistic traffic against the highest-volume read
// endpoint (GET /trips/search). Confirms latency stays acceptable and the
// Redis cache-aside layer is actually absorbing repeat queries — watch the
// p95 drop sharply after the first ~30s once the cache is warm.
//
// Run:  k6 run search-trips-load-test.js
// Run against a specific host: k6 run -e BASE_URL=http://localhost:8080 search-trips-load-test.js
import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';

const cacheMissLikelyDuration = new Trend('search_duration_cold', true);
const cacheHitLikelyDuration = new Trend('search_duration_warm', true);

export const options = {
  scenarios: {
    steady_search_traffic: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '30s', target: 20 },  // ramp up
        { duration: '2m', target: 20 },   // hold — cache should be fully warm by now
        { duration: '30s', target: 0 }    // ramp down
      ]
    }
  },
  thresholds: {
    http_req_failed: ['rate<0.01'],       // <1% errors
    http_req_duration: ['p(95)<400'],     // 95% of requests under 400ms
    'search_duration_warm': ['p(95)<150'] // once cached, should be fast
  }
};

const ROUTES = [
  ['Dhaka', 'Chattogram'],
  ['Dhaka', 'Sylhet'],
  ['Chattogram', 'Cox\'s Bazar']
];

export default function () {
  const [origin, destination] = ROUTES[Math.floor(Math.random() * ROUTES.length)];
  const date = '2026-08-15'; // fixed date -> same cache key across VUs, exercising the cache deliberately

  const res = http.get(
    `${BASE_URL}/api/v1/trips/search?origin=${origin}&destination=${destination}&date=${date}&page=1&pageSize=20`
  );

  check(res, {
    'status is 200': (r) => r.status === 200,
    'has items array': (r) => {
      try {
        return Array.isArray(JSON.parse(r.body).items);
      } catch {
        return false;
      }
    }
  });

  // First iterations per VU are more likely cold; rough heuristic split for
  // the two trend metrics using k6's iteration counter.
  if (__ITER < 2) {
    cacheMissLikelyDuration.add(res.timings.duration);
  } else {
    cacheHitLikelyDuration.add(res.timings.duration);
  }

  sleep(Math.random() * 1.5 + 0.5); // think time, 0.5-2s
}
