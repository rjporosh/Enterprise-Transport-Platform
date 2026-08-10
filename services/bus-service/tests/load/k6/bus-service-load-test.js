import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
  scenarios: {
    read_heavy: {
      executor: 'constant-arrival-rate',
      rate: 20,
      timeUnit: '1s',
      duration: '30s',
      preAllocatedVUs: 10,
      maxVUs: 50,
      exec: 'get_buses'
    },
    write_burst: {
      executor: 'constant-arrival-rate',
      rate: 2,
      timeUnit: '1s',
      duration: '15s',
      preAllocatedVUs: 5,
      maxVUs: 20,
      exec: 'register_bus'
    },
    cache_hit: {
      executor: 'constant-vus',
      vus: 10,
      duration: '20s',
      exec: 'get_single_bus'
    }
  },
  thresholds: {
    'http_req_duration{scenario:read_heavy}': ['p(95)<200'],
    'http_req_duration{scenario:write_burst}': ['p(95)<500'],
    'http_req_duration{scenario:cache_hit}': ['p(95)<100'],
    'http_req_failed{scenario:read_heavy}': ['rate<0.01'],
    'http_req_failed{scenario:write_burst}': ['rate<0.01']
  }
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5201';
const AUTH_TOKEN = __ENV.AUTH_TOKEN || '';
const DEPOT_ID = __ENV.DEPOT_ID || '';

export function get_buses() {
  const res = http.get(`${BASE_URL}/api/v1/buses?page=1&pageSize=20`, {
    headers: { Authorization: `Bearer ${AUTH_TOKEN}` }
  });
  check(res, { 'status is 200': (r) => r.status === 200 });
  sleep(1);
}

export function register_bus() {
  const plate = `LD${Math.random().toString(36).slice(2, 8).toUpperCase()}`;
  const payload = JSON.stringify({
    operatorId: __ENV.OPERATOR_ID || '00000000-0000-0000-0000-000000000000',
    plateNumber: plate,
    busType: 'AcSleeper',
    totalSeats: 40,
    depotId: DEPOT_ID
  });

  const res = http.post(`${BASE_URL}/api/v1/buses`, payload, {
    headers: {
      Authorization: `Bearer ${AUTH_TOKEN}`,
      'Content-Type': 'application/json'
    }
  });
  check(res, { 'status is 200': (r) => r.status === 200 });
  sleep(1);
}

export function get_single_bus() {
  const busId = __ENV.BUS_ID;
  if (!busId) return;

  const res = http.get(`${BASE_URL}/api/v1/buses/${busId}`, {
    headers: { Authorization: `Bearer ${AUTH_TOKEN}` }
  });
  check(res, { 'status is 200': (r) => r.status === 200 });
  sleep(0.5);
}
