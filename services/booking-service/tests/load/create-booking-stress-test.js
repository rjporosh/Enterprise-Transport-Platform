// k6 STRESS test with an adversarial shape: many virtual users all try to
// book the SAME seat on the SAME trip at (as close to) the same instant.
// This is the test that actually exercises CreateBookingHandler's
// concurrency control (Trip.HoldSeats + Postgres xmin optimistic
// concurrency) under real contention, not just unit-test mocks.
//
// Expected, correct result: exactly ONE request per contested seat returns
// 201; every other concurrent request for that same seat returns 409. If
// you see more than one 201 for the same seat, that's a real correctness
// bug in the concurrency control — this test exists to catch exactly that.
//
// Prerequisite: seed a trip with a small number of seats (see
// scripts/seed-demo-data.sql) and pass its id in; a trip with only 3-4
// seats maximizes contention for a given VU count.
//
// Run:
//   k6 run \
//     -e BASE_URL=http://localhost:8080 \
//     -e TRIP_ID=<seed-a-real-trip-id> \
//     -e ACCESS_TOKEN=<a-valid-dev-jwt> \
//     create-booking-stress-test.js
import http from 'k6/http';
import { check } from 'k6';
import { Counter } from 'k6/metrics';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';
const TRIP_ID = __ENV.TRIP_ID || '00000000-0000-0000-0000-000000000000';
const ACCESS_TOKEN = __ENV.ACCESS_TOKEN || '';
const SEAT_NUMBER = __ENV.SEAT_NUMBER || 'A1';

const successfulBookings = new Counter('successful_bookings_for_contested_seat');
const conflictResponses = new Counter('seat_conflict_409_responses');
const unexpectedErrors = new Counter('unexpected_error_responses');

export const options = {
  scenarios: {
    seat_contention_spike: {
      executor: 'per-vu-iterations',
      vus: 50,          // 50 concurrent customers...
      iterations: 1,     // ...each trying exactly once...
      maxDuration: '30s' // ...as close to simultaneously as k6 can schedule.
    }
  },
  thresholds: {
    // The correctness assertion: across all 50 attempts at the same seat,
    // exactly one may succeed.
    successful_bookings_for_contested_seat: ['count<=1']
  }
};

export default function () {
  const payload = JSON.stringify({
    tripId: TRIP_ID,
    customerId: `00000000-0000-0000-0000-${String(__VU).padStart(12, '0')}`,
    passengers: [
      { seatNumber: SEAT_NUMBER, fullName: `Load Test VU ${__VU}`, age: 30, gender: 'Male' }
    ]
  });

  const res = http.post(`${BASE_URL}/api/v1/bookings`, payload, {
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${ACCESS_TOKEN}`
    }
  });

  if (res.status === 201) {
    successfulBookings.add(1);
  } else if (res.status === 409) {
    conflictResponses.add(1);
  } else {
    unexpectedErrors.add(1);
  }

  check(res, {
    'status is 201 or 409 (never anything else for a contested seat)': (r) => r.status === 201 || r.status === 409
  });
}
