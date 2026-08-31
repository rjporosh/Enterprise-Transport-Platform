# Release Notes — Enterprise Transport Platform

Platform-wide release notes. Frontend apps also keep their own
(`apps/*/release-notes.md`).

---

## M0 — Shared kernel + YARP API gateway — 2026-09-01

Milestone M0 of the production hardening plan
(`docs/PRODUCTION-MILESTONES.md`). One commit:
`feat(platform): implement M0 shared kernel and YARP gateway`.

### Added

- **`shared/shared-kernel`** (`Platform.SharedKernel`) — cross-cutting primitives:
  `PlatformHeaders`, `CorrelationId` (validate/generate), `CorrelationContext`
  (`AsyncLocal` ambient id), `TenantContext` + `ITenantContextAccessor`,
  `Error`/`Result`/`Result<T>`, `ApiResponse<T>`, `PageRequest`/`PagedResult<T>`,
  `IdempotencyStore` contract, `RequestMetadata`.
- **`shared/contracts`** (`Platform.Contracts`) — `EventTypes` (all RabbitMQ
  routing keys as constants), `EventTypeRegistry` (every domain event → key),
  `IntegrationEventRoutingKeys` resolver, versioned `IntegrationEvents.*V1`
  contract records.
- **`shared/common`** (`Platform.Common`) — reusable ASP.NET middleware:
  `CorrelationIdMiddleware`, `CorrelationPropagationHandler` (outbound
  `DelegatingHandler`), `SecurityHeadersMiddleware`, `TenantHeaderHygieneMiddleware`.
- **`infrastructure/gateway`** (`Platform.Gateway`) — a production-grade YARP
  reverse proxy: the single public API entry point. Config-driven routes (16) and
  clusters (7 — six services + a reserved `ticketing` cluster for M6). Correlation
  ingress + forwarding, tenant-header hygiene, edge rate limiting
  (IP/user/tenant-partitioned), security headers, 10 MB body cap, per-cluster
  timeouts, passive health checks, Serilog + OpenTelemetry + `/metrics`. Dockerfile
  (non-root `app` user, `HEALTHCHECK`). Added to `docker-compose.yml` as
  `api-gateway` (host port 8088).
- New tests: `Platform.Contracts.Tests` (226), `Platform.Gateway.Tests` (21),
  `Platform.Messaging.IntegrationTests` (4, real RabbitMQ container).
- New solutions: `shared/Platform.Shared.sln`, `infrastructure/gateway/Platform.Gateway.sln`.
- Docs: `docs/programmers-guide/{gateway,messaging-contracts,correlation-id}.md`;
  a full "first use" walkthrough in `guide.md`.
- Angular `environment.staging.ts` + `angular.json` staging config; React
  `.env.{development,staging,production}`.

### Fixed

- **RabbitMQ routing keys (P0-4).** Every service derived its routing key by
  string-munging the stored CLR type name. **Root cause:** the derivation
  prepended `<service>.` to a name that already started with the entity name
  (`BookingConfirmed` → `booking.confirmed` → `booking.` + that = `booking.booking.confirmed`),
  and payment split an `AssemblyQualifiedName` on `.` producing
  `payment.<culture=neutral, PublicKeyToken=null>`. The notification consumer binds
  to `booking.confirmed` / `payment.succeeded`, so **booking-confirmation and
  payment-receipt notifications could never be delivered.**
  **Fix:** all six `OutboxProcessor`s and payment's `FailedWebhookRetryJob` now
  call `IntegrationEventRoutingKeys.Resolve(message.EventType, "<service>")`, which
  looks the event up in the explicit `Platform.Contracts.EventTypeRegistry` (the
  `auth.*` keys are pinned to their current values — zero change for auth) and
  falls back to a deterministic, no-double-prefix key with a warning for anything
  unregistered. No `ToRoutingKey` / `DeriveRoutingKey` / AssemblyQualifiedName-as-
  routing-key remains anywhere.
- **Tenant-header spoofing at the edge (partial P0-11).** The gateway now strips
  any client-supplied `X-Tenant-Id` / `X-Company-Id` / `X-Organization-Id` and
  re-injects them only from a validated JWT claim. Verified end-to-end.
- **`ForwardedHeadersOptions.KnownNetworks` deprecation** and a dead local in
  `SecurityHeadersMiddleware`.

### Changed

- **Frontend networking.** Angular `proxy.conf.json` + `nginx.conf`, React
  `vite.config.ts` + `nginx.conf` collapsed from per-service path fan-out to a
  single upstream: the gateway. `environment*.ts` / `.env*` / `env.ts` comments
  updated. Repo-wide grep confirms **no direct internal-service URL or port**
  remains in either app.
- `docker-compose.yml`: `customer-web` / `admin-console` now `depends_on:
  [api-gateway]`.
- All six service `.sln` files gained `Platform.Contracts` + `Platform.SharedKernel`.
- Correlation: all six `RabbitMqPublisher`s set `IBasicProperties.CorrelationId`
  from the ambient `CorrelationContext` when present.

### Correlation propagation

Gateway → HTTP-service is done and verified. RabbitMQ carries the correlation id
**only when a synchronous publish has an ambient value** — carrying it *through
the transactional outbox* needs a persisted `OutboxMessage.CorrelationId` column
and is deferred to **M2/M9**.

### Security notes

- The gateway **refuses to start in `Production` without `Jwt:SigningKey`** (it
  reads claims for tenant propagation and rate-limit partitioning — it must
  validate the signature first).
- `infrastructure/gateway/src/Platform.Gateway/appsettings.Development.json`
  carries the same dev-only signing key already committed in `docker-compose.yml`
  for all services. This is dev-only; **per-service production keys are milestone M11.**
- Gateway rate limiter is **in-memory** — not multi-instance safe. Redis-backed
  distributed limiting is milestone **M9**. The partitioning (tenant→user→IP) and
  policy names will not change.

### Verification performed

- Build: all 6 service solutions + `Platform.Shared.sln` + `Platform.Gateway.sln`
  — **0 errors**.
- Tests: **155/155 existing unit tests green** (no regression); **251 new tests
  green** (226 contract + 21 gateway + 4 messaging-integration).
- The 2 pre-existing `AuthService.IntegrationTests` failures
  (`Admin_ListPermissions_ReturnsSuccess`, `SecurityQuestions_ConfigureAndVerify_ReturnsSuccess`)
  were confirmed present on the clean pre-M0 baseline — **not caused by M0**.
- Gateway Docker image builds (372 MB), runs as non-root `app` (uid 1654),
  `/health` → 200.
- End-to-end (real gateway image + stub backend): `/api/v1/auth/*` routed to the
  `auth` cluster; `X-Correlation-Id` preserved; `X-Forwarded-By-Gateway` injected;
  **client `X-Tenant-Id` stripped**.
- Frontends: `npm run build` succeeds for both apps (production + Angular staging).
- `docker compose config` valid.

### Known limitations / deferred

See `docs/PRODUCTION-MILESTONES.md` "Deferred" list under M0. Headline:
Redis rate limiting (M9), observability backend (M8), outbox correlation column
(M2/M9), consumer inbox de-dup (M7), pre-existing service Dockerfile `useradd`
bug (M11), booking `InitialCreate` migration (M2).

### Operational notes

- New host port in use: **8088** (api-gateway).
- Frontends now require the gateway to be running (Docker: automatic;
  local dev: `dotnet run --project infrastructure/gateway/src/Platform.Gateway`).
- No database schema change in this milestone. No migration to apply.
