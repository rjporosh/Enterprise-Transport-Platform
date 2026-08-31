# Correlation ID

**Status:** gateway → HTTP-service propagation implemented in **M0**.
RabbitMQ + background-job propagation partially done (see "Deferred" below).

---

## Concept

- **Correlation ID** (`X-Correlation-Id`) — one *logical operation*, potentially
  many spans and services. Human-usable, appears in every log line, safe to show
  a support agent.
- **Trace ID** (`traceparent`, W3C) — handled by OpenTelemetry auto-instrumentation.
  Different concept; do not conflate. The gateway and services never strip
  `traceparent`.

Correlation ids come from untrusted callers, so they are **validated, never
blindly trusted** (`Platform.SharedKernel.Correlation.CorrelationId.IsValid`):
8–128 chars, `[A-Za-z0-9._:-]` only. Anything else is replaced with a fresh one.

---

## The building blocks (`Platform.SharedKernel` / `Platform.Common`)

| Type | Purpose |
|------|---------|
| `PlatformHeaders` | Canonical header-name constants (`X-Correlation-Id`, `X-Tenant-Id`, …). |
| `CorrelationId` | `New()`, `IsValid(s)`, `NormalizeOrCreate(s)`. |
| `CorrelationContext` | Ambient id for the current async flow, backed by `AsyncLocal<string?>`. Set once at the edge via `BeginScope`; read anywhere downstream via `Current`. Replaces the racy `static string` fields the audit flagged (P1-16). |
| `CorrelationIdMiddleware` (Platform.Common) | Reads/normalises/generates the header, publishes to `CorrelationContext`, pushes it into the Serilog scope, echoes it on the response. Register **first** in the pipeline. |
| `CorrelationPropagationHandler` (Platform.Common) | Outbound `DelegatingHandler` — stamps `CorrelationContext.Current` onto every request a typed `HttpClient` makes. |

---

## What flows today (M0)

```
Browser
  │  (nginx forwards X-Correlation-Id as-is)
  ▼
API Gateway ── CorrelationIdMiddleware: validate / generate ──► CorrelationContext + response header
  │  YARP request transform re-writes X-Correlation-Id on the proxied request
  ▼
Backend service ── its own CorrelationIdMiddleware reads the header ──► its logs + response
```

Verified end-to-end (gateway Docker image + a stub backend):
`X-Correlation-Id` supplied by the client is preserved on the request the
service receives; when absent the gateway generates one and returns it.

`RabbitMqPublisher` in all six services now sets `IBasicProperties.CorrelationId`
from `CorrelationContext.Current` **when a value is present**.

---

## Deferred (not done in M0)

| Gap | Why | Milestone |
|-----|-----|-----------|
| Correlation id carried **through the transactional outbox** to RabbitMQ | The `OutboxProcessor` runs in a background service with no ambient `CorrelationContext`; carrying the originating request's id needs a persisted `OutboxMessage.CorrelationId` column (a schema change per service; booking-service has no migrations yet). | **M2** (booking `InitialCreate` + column) / **M9** (all services + outbox hardening) |
| `CorrelationPropagationHandler` wired into every service's outbound `HttpClient`s | Requires touching each service's DI; not in M0's "no unrelated rewrite" scope. | M9 |
| Consuming `IBasicProperties.CorrelationId` in `NotificationEventConsumer` and re-establishing the scope | Comes with the inbox/de-dup work. | M7 |
| Correlation scope inside Quartz jobs | Job-runner change per service. | M9 |
| Services adopting the shared `CorrelationIdMiddleware` (replacing their six copies) | Incremental migration; behaviour is already equivalent. | ongoing |

---

## Verifying

```bash
# generated when absent
curl -sD - -o /dev/null http://localhost:8088/ | grep -i x-correlation-id

# preserved when valid
curl -sD - -o /dev/null -H 'X-Correlation-Id: order-42-abc' http://localhost:8088/api/v1/auth/me | grep -i x-correlation-id

# replaced when malformed
curl -sD - -o /dev/null -H 'X-Correlation-Id: has spaces!' http://localhost:8088/ | grep -i x-correlation-id
```

Automated: `Platform.Gateway.Tests` (generate/preserve/replace + forwarded to a
stub downstream) and `Platform.Messaging.IntegrationTests`
(`CorrelationId` survives a publish onto a real RabbitMQ queue).
