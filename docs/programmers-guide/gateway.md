# API Gateway (YARP)

**Status:** implemented in milestone **M0** (commit introducing `infrastructure/gateway/`).
**Project:** `infrastructure/gateway/src/Platform.Gateway`
**Solution:** `infrastructure/gateway/Platform.Gateway.sln`
**Container:** `api-gateway` in `infrastructure/docker/docker-compose.yml`, host port **8088** → container **8080**.

---

## What it is

The single public entry point for the platform. Angular (customer) and React
(admin) call **one base URL** — the gateway — and the gateway routes each
`/api/v1/*` path prefix to the right backend service. Browsers never see an
individual service URL.

It is a **thin edge**, not a service:

| Does | Does NOT |
|------|----------|
| Reverse-proxy routing (path-prefix → cluster) | Any business logic |
| Correlation-id ingress (generate / validate / propagate) | Persist anything |
| Strip client-supplied tenant headers; re-inject from the JWT claim | Make authorization decisions (services do) |
| Edge rate-limit backstop (IP / user / tenant partitioned) | Aggregate or transform responses |
| Security response headers, request-size cap, per-cluster timeouts | Terminate TLS in dev (an ingress/LB owns that in prod) |
| Passive health-aware routing | Own tenant/subscription state |

Built on **YARP 2.3.0** (`Yarp.ReverseProxy`). Routes and clusters are **config
only** (`appsettings.json` → `ReverseProxy` section) — no code change to add or
move a route.

---

## Routes

All under `/api/v1`. Path prefix → cluster → service:

| Path prefix | Cluster | Service | Rate-limit policy |
|-------------|---------|---------|-------------------|
| `/api/v1/auth/**` | `auth` | auth-service | `gateway-auth` (stricter) |
| `/api/v1/admin/**` | `auth` | auth-service | `gateway-global` |
| `/api/v1/bookings/**`, `/api/v1/trips/**` | `booking` | booking-service | `gateway-global` |
| `/api/v1/buses/**`, `/api/v1/depots/**` | `bus` | bus-service | `gateway-global` |
| `/api/v1/routes/**`, `/api/v1/stops/**`, `/api/v1/schedules/**` | `route` | route-service | `gateway-global` |
| `/api/v1/payments/**`, `/api/v1/agents/**`, `/api/v1/webhooks/**` | `payment` | payment-service | `gateway-payment` |
| `/api/v1/notifications/**`, `/api/v1/templates/**`, `/api/v1/recipients/**` | `notification` | notification-service | `gateway-global` |
| `/api/v1/tickets/**` | `ticketing` | **(not built yet — milestone M6)** → 502 | `gateway-global` |

The gateway's own endpoints: `GET /` (info), `GET /health`, `GET /metrics`
(Prometheus).

---

## Configuration

Internal service addresses are **not hard-coded**. They come from config and are
overridable by environment variable (double-underscore = colon):

```
ReverseProxy__Clusters__auth__Destinations__primary__Address = http://auth-service:8080/
ReverseProxy__Clusters__booking__Destinations__primary__Address = http://booking-service:8080/
...one per cluster...
```

`appsettings.json` (Production default) points at Docker DNS names.
`appsettings.Development.json` points at the local `dotnet run` ports
(`http://localhost:5101` … `5601`).

Other settings:

| Key | Default | Meaning |
|-----|---------|---------|
| `Jwt:SigningKey` | `""` (Production) / dev key (Development) | HMAC key to validate tokens. **Required in Production — the gateway refuses to start without it** (it reads claims for tenant propagation + rate-limit partitioning). |
| `Jwt:Issuer` / `Jwt:Audience` | platform values | Token validation parameters. |
| `Gateway:MaxRequestBodyBytes` | `10485760` (10 MB) | Kestrel request-body cap. |
| `Gateway:ForwardedHeaders:Enabled` | `false` | When `true`, honour `X-Forwarded-For` **only from** `Gateway:ForwardedHeaders:KnownProxies` (list of ingress/LB IPs). Off by default so the gateway rate-limits on the real socket peer. |
| `Gateway:RateLimiting:WindowSeconds` | `60` | Fixed window. |
| `Gateway:RateLimiting:GlobalPermitLimit` | `300` | Per partition per window. |
| `Gateway:RateLimiting:AuthPermitLimit` | `20` | Login/OTP/reset backstop. |
| `Gateway:RateLimiting:PaymentPermitLimit` | `60` | Payment + webhook backstop. |
| `OpenTelemetry:OtlpEndpoint` | `http://localhost:4317` | Trace exporter target (no collector deployed yet — milestone M8). |

### Rate-limit partitioning

Partition key is resolved in order: **tenant id claim → user (`sub`) claim →
client IP**, prefixed by the policy bucket so buckets don't share a budget.
Not MAC-address based (browsers can't expose a MAC).

This is an **abuse/DoS backstop only** — the plan-aware per-tenant quota system
is milestone **M10** (ADR-0009). The store is currently **in-memory fixed
window**; milestone **M9** swaps it for a Redis-backed distributed limiter
(partition keys and policy names stay the same).

---

## Running it

### Local (`dotnet run`)

```bash
cd ~/Downloads/porosh/Enterprise-Transport-Platform
dotnet run --project infrastructure/gateway/src/Platform.Gateway
# listens on http://localhost:8080  (see Properties/launchSettings.json)
```

It reads `appsettings.Development.json`, so it expects the six services on their
local ports (`dotnet run` each service, or run the Docker stack and point the
Development cluster addresses at the published ports).

### Docker Compose (recommended)

```bash
cd infrastructure/docker
docker compose up -d --build
# gateway:        http://localhost:8088
# customer web:   http://localhost:4200  (its nginx proxies /api/v1 -> api-gateway:8080)
# admin console:  http://localhost:5173  (same)
```

### Docker image directly

```bash
# build context MUST be the repo root (gateway references shared/*)
docker build -f infrastructure/gateway/Dockerfile -t platform-gateway .
docker run -p 8088:8080 -e Jwt__SigningKey=... platform-gateway
```

The image runs as the non-root `app` user (uid 1654) and has a `HEALTHCHECK`
against `/health`.

---

## Verifying it

```bash
# health
curl -s -o /dev/null -w '%{http_code}\n' http://localhost:8088/health      # 200

# correlation id is generated when absent, and returned on the response
curl -sD - -o /dev/null http://localhost:8088/ | grep -i x-correlation-id

# a client-supplied correlation id is preserved
curl -sD - -o /dev/null -H 'X-Correlation-Id: my-trace-123' http://localhost:8088/ | grep -i x-correlation-id

# a malformed correlation id is replaced (not echoed back)
curl -sD - -o /dev/null -H 'X-Correlation-Id: bad id !!' http://localhost:8088/ | grep -i x-correlation-id

# security headers, no Server header
curl -sD - -o /dev/null http://localhost:8088/ | grep -iE 'x-frame-options|content-security-policy|^server:'

# routing: reaches auth-service (401/400/whatever the service returns = routing OK)
curl -s -o /dev/null -w '%{http_code}\n' http://localhost:8088/api/v1/auth/me

# a client X-Tenant-Id is stripped before the request reaches the service
#   (confirmed by the gateway test suite; see below)
```

Automated coverage: `infrastructure/gateway/tests/Platform.Gateway.Tests`
(`dotnet test infrastructure/gateway/Platform.Gateway.sln`) — 21 tests covering
correlation, tenant-strip, security headers, health/metrics, and proxying to a
stub downstream.

---

## Deferred to later milestones

| Item | Milestone |
|------|-----------|
| Redis-backed distributed rate limiter | M9 |
| Per-tenant / plan-aware quotas (ADR-0009) | M10 |
| OTLP collector + Jaeger + Grafana behind the exporter | M8 |
| `ticketing` cluster gets a real service | M6 |
| Per-service (non-shared) JWT signing keys | M11 |
| Active (not just passive) YARP health checks enabled | M8/M9 |
