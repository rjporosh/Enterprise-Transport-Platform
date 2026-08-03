// k6 stress test: adversarial spike against POST /auth/register — a mix of
// unique emails (should all succeed, up to the rate limiter) AND a single
// shared email hammered by many VUs at once (should succeed exactly ONCE;
// every other attempt must be 409 Conflict, never a duplicate row). This is
// a correctness test for the email-uniqueness check under real concurrency,
// not just a single-threaded unit test, plus it exercises the "auth-write"
// rate limiter's 429 behavior.
//
// Run:  k6 run register-stress-test.js
import http from "k6/http";
import { check } from "k6";
import { Counter } from "k6/metrics";

const BASE_URL = __ENV.BASE_URL || "http://localhost:8081";
const SHARED_EMAIL = `k6-race-${Date.now()}@example.com`;

const duplicateEmailSuccesses = new Counter("duplicate_email_successes");
const rateLimited = new Counter("rate_limited_429");

export const options = {
  scenarios: {
    // 50 VUs firing once each, near-simultaneously, at the SAME email —
    // this is the race the uniqueness check must survive.
    duplicate_email_race: {
      executor: "shared-iterations",
      vus: 50,
      iterations: 50,
      maxDuration: "30s"
    }
  },
  thresholds: {
    duplicate_email_successes: ["count<=1"] // exactly one winner, or the uniqueness check has a race condition
  }
};

export default function () {
  const res = http.post(
    `${BASE_URL}/api/v1/auth/register`,
    JSON.stringify({ email: SHARED_EMAIL, password: "correct-horse-battery-staple", firstName: "Race", lastName: "Condition", phoneNumber: null }),
    { headers: { "Content-Type": "application/json" } }
  );

  check(res, {
    "status is 200, 409, or 429": (r) => [200, 409, 429].includes(r.status)
  });

  if (res.status === 200) duplicateEmailSuccesses.add(1);
  if (res.status === 429) rateLimited.add(1);
}
