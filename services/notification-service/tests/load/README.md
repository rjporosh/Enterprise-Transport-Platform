# Load & stress tests

## What's here

- `k6/send-notification-load-test.js` — sustained load against
  `POST /api/v1/notifications`, asserting **p95 < 300ms** and **&lt;1% errors**
  at 25 concurrent VUs for 2 minutes. That tight a latency threshold is only
  meaningful *because* SendNotification never calls an email/SMS/push
  provider inline (see `SendNotificationHandler`'s remarks) — it just
  inserts a row and enqueues an outbox event, so p95 staying low is a direct
  check that the async-dispatch design is actually working, not an
  optimistic guess.

```bash
cd k6
k6 run -e BASE_URL=http://localhost:5301 send-notification-load-test.js
# No local install: docker run --rm -i grafana/k6 run - < send-notification-load-test.js
```

## `jmeter/` and `nbomber/` — intentionally empty

AuthService's load suite (see `services/auth-service/tests/load/`) ships the
same scenarios in all three tools. For this delivery, only k6 is included —
porting the identical scenario to a JMeter `.jmx` plan and an NBomber `.csproj`
is mechanical but adds two more artifacts this sandbox has no way to actually
run (no JMeter/`.NET` load-test execution here, same as the rest of this
service — see the final delivery report's Known Limitations). Rather than
check in unverified JMeter XML or an NBomber project that might not compile,
the k6 script above is the one, real, immediately runnable load test; add the
other two following AuthService's existing files as a template if your team
standardizes on them.

## Prerequisites

A running instance of the service (`dotnet run` from `src/NotificationService.Api`,
or `docker compose up notification-service`) reachable at `BASE_URL`, with a
real Postgres and RabbitMQ behind it — see the main repo's
`infrastructure/docker/docker-compose.yml`.
