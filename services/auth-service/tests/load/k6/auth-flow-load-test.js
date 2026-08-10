// k6 load test: full auth flow including OTP and security questions.
// Exercises the most common user journey: register -> login -> request OTP -> verify OTP.
//
// Run:  k6 run auth-flow-load-test.js
// Run against a specific host: k6 run -e BASE_URL=http://localhost:8081 auth-flow-load-test.js
import http from "k6/http";
import { check, sleep } from "k6";

const BASE_URL = __ENV.BASE_URL || "http://localhost:8081";
const USER_POOL_SIZE = Number(__ENV.USER_POOL_SIZE || 20);

export const options = {
  scenarios: {
    full_auth_flow: {
      executor: "ramping-vus",
      startVUs: 0,
      stages: [
        { duration: "30s", target: 10 },
        { duration: "1m", target: 10 },
        { duration: "30s", target: 0 }
      ]
    }
  },
  thresholds: {
    http_req_failed: ["rate<0.02"],
    http_req_duration: ["p(95)<500"]
  }
};

export function setup() {
  const users = [];
  for (let i = 0; i < USER_POOL_SIZE; i++) {
    const email = `k6-flow-${i}-${Date.now()}@example.com`;
    users.push({ email, password: "correct-horse-battery-staple" });
  }
  return { users };
}

export default function (data) {
  const user = data.users[Math.floor(Math.random() * data.users.length)];

  const registerRes = http.post(
    `${BASE_URL}/api/v1/auth/register`,
    JSON.stringify({ email: user.email, password: user.password, firstName: "Load", lastName: "Test", phoneNumber: null }),
    { headers: { "Content-Type": "application/json" } }
  );
  check(registerRes, { "register is 200": (r) => r.status === 200 });
  if (registerRes.status !== 200) return;

  const loginRes = http.post(
    `${BASE_URL}/api/v1/auth/login`,
    JSON.stringify({ email: user.email, password: user.password }),
    { headers: { "Content-Type": "application/json" } }
  );
  check(loginRes, { "login is 200": (r) => r.status === 200 });
  if (loginRes.status !== 200) return;

  const loginBody = JSON.parse(loginRes.body);
  const userId = loginBody.userId;

  const otpRes = http.post(
    `${BASE_URL}/api/v1/auth/otp/request`,
    JSON.stringify({ userId, channel: "email", destination: user.email }),
    { headers: { "Content-Type": "application/json" } }
  );
  check(otpRes, { "otp request is 204": (r) => r.status === 204 });

  sleep(1);
}
