// k6 load test: steady, realistic sign-in traffic against POST /auth/login.
// A pool of pre-registered users is created once in setup() and reused by
// all VUs, so this measures real login latency (PBKDF2 verify + JWT issue +
// refresh-token insert), not registration overhead.
//
// Run:  k6 run login-load-test.js
// Run against a specific host: k6 run -e BASE_URL=http://localhost:8081 login-load-test.js
import http from "k6/http";
import { check, sleep } from "k6";

const BASE_URL = __ENV.BASE_URL || "http://localhost:8081";
const USER_POOL_SIZE = Number(__ENV.USER_POOL_SIZE || 50);
const PASSWORD = "correct-horse-battery-staple";

export const options = {
  scenarios: {
    steady_login_traffic: {
      executor: "ramping-vus",
      startVUs: 0,
      stages: [
        { duration: "30s", target: 25 }, // ramp up
        { duration: "2m", target: 25 },  // hold
        { duration: "30s", target: 0 }   // ramp down
      ]
    }
  },
  thresholds: {
    http_req_failed: ["rate<0.01"],    // <1% errors
    http_req_duration: ["p(95)<400"]   // 95% of logins under 400ms
  }
};

// setup() runs once, before VUs start — registers the user pool.
export function setup() {
  const emails = [];
  for (let i = 0; i < USER_POOL_SIZE; i++) {
    const email = `k6-login-${i}-${Date.now()}@example.com`;
    const res = http.post(
      `${BASE_URL}/api/v1/auth/register`,
      JSON.stringify({ email, password: PASSWORD, firstName: "Load", lastName: `Test${i}`, phoneNumber: null }),
      { headers: { "Content-Type": "application/json" } }
    );
    if (res.status === 200) emails.push(email);
  }
  return { emails };
}

export default function (data) {
  const email = data.emails[Math.floor(Math.random() * data.emails.length)];

  const res = http.post(
    `${BASE_URL}/api/v1/auth/login`,
    JSON.stringify({ email, password: PASSWORD }),
    { headers: { "Content-Type": "application/json" } }
  );

  check(res, {
    "status is 200": (r) => r.status === 200,
    "has access token": (r) => {
      try {
        return typeof JSON.parse(r.body).accessToken === "string";
      } catch {
        return false;
      }
    }
  });

  sleep(Math.random() * 1.5 + 0.5); // think time, 0.5-2s
}
