# Release Notes — Enterprise Transport Platform

Platform-wide release notes. Frontend apps also keep their own
(`apps/*/release-notes.md`).

---

## MVP-3 (roadmap M6) — Ticketing Service: real ticket + QR + PDF — 2026-09-03

Commit: `feat(ticketing): M6 — new service: ticket issuance, QR, QuestPDF, templates`.

### Added — new `services/ticketing-service`

- Consumes `booking.confirmed` → issues a `Ticket` with a checksummed number
  (`TKT-YYMMDD-XXXXXX-C`) and an opaque verification code; idempotent + inbox-deduplicated.
- Renders a print-ready **A5 PDF** (QuestPDF) with a QR that resolves to
  `GET /api/v1/tickets/verify/{code}`; template-driven branding (colours, logo, terms).
- Emits `ticket.issued` on `ticket.events` (carries contact + PDF URL for notification).
- Endpoints: `GET /api/v1/tickets/{mine,{id},{id}/pdf}`, public
  `GET /api/v1/tickets/verify/{code}`, `POST /tickets/{id}/{cancel,reissue}`,
  `GET/POST/PUT /api/v1/ticket-templates` + `POST /ticket-templates/{id}/logo`.
- DB-provider factory (Postgres default), outbox + inbox, health checks,
  OpenTelemetry, Serilog, Scalar. `InitialCreate` migration (schema `ticketing`).
- `docker-compose.yml`: `postgres-ticketing` (:5438) + `ticketing-service` (:5205);
  gateway `ticketing` cluster wired (routes were already reserved).
- `docs/programmers-guide/ticketing.md`.

### Changed (additive)

- `booking.confirmed` / `BookingConfirmedV1` gain `OperatorId` (for per-operator
  templates). `TicketIssuedV1` enriched with contact + `PdfUrl`.

### Operational notes

- New DB `ticketing_service` — run its migration.
- QuestPDF Community licence set at startup; native deps `libfontconfig1` +
  `libfreetype6` are in the Dockerfile.
- Reissue = reprint: keeps the ticket number + verification code.

---

## MVP-2 (roadmap M3 + genuine QR) — payment safety + Bangla-QR — 2026-09-03

Commit: `feat(payment): M3 — confirm/webhook/refund safety + genuine EMVCo Bangla-QR provider`.

### Added

- **Genuine EMVCo / "Bangla QR" payment** — `PaymentMethodType.Qr` +
  `QrPaymentProvider`. Spec-correct merchant-presented QR payload (TLV, MCC 4131,
  BDT, CRC-16), rendered to a PNG. `POST /api/v1/payments/{id}/qr` returns the
  payload + image + expiry; any bank / MFS app scans it.
- QR settlement: signed `POST /api/v1/webhooks/qr` (HMAC-SHA256) or audited admin
  `POST /api/v1/payments/{id}/settle-qr` → `payment.succeeded` → booking confirmed.
- `docs/programmers-guide/payments-qr.md`; `Payments:Qr` config block.

### Fixed / hardened

- **`ConfirmPayment` no longer trusts the request body** (P0-5) — a payment
  succeeds only on a server-side `provider.GetStatusAsync`. A forged `/confirm`
  now returns 400.
- **Webhook forgery** (P0-6) — `DefaultPaymentProvider` fails closed
  (`VerifyWebhookSignature` → false, `ConfirmAsync` → Unknown). Unknown provider /
  bad signature / bad payload → 400.
- **Refunds call the PSP** (P0-7) — `RefundPaymentHandler` invokes
  `provider.RefundAsync`; `Payment.Status` moves to PartiallyRefunded / Refunded
  **only when a refund actually settles**, and then publishes `payment.refunded`.
  A rejected refund leaves the payment untouched.
- `CreatePayment` sources tenant + customer id from the token, not the body.

### Changed (contracts / domain — additive)

- `Payment` gains `SettledRefundedAmount` + `ApplyRefundSettlement()`;
  `InitiateRefund` no longer flips status inline.
- `PaymentRefundedDomainEvent` is now actually raised (was dead).

### Operational notes

- No new migration. `Payments:Qr:WebhookSigningKey` empty ⇒ the QR webhook rejects
  all calls; settle via the audited admin endpoint until an acquirer is wired.
- bKash / Nagad remain credential-gated (no fake success); Nagad still needs the
  real DFS envelope (roadmap M5).

---

## MVP-1 (roadmap M2 + M1 slice) — booking-service end-to-end — 2026-09-03

Commit: `feat(booking): M2 — migrations, read-model consumers, trip mgmt, payment-driven confirm`.

### Fixed

- **booking-service had no EF migrations** (P0-3) — startup `MigrateAsync()` was a
  no-op, the schema was never created, every request failed. Added
  `20260903113152_InitialCreate` (schema `booking`).
- **No payment → booking-confirmation path** — a paid booking stayed `PendingPayment`
  forever. Added `PaymentEventConsumer`: `payment.succeeded` → confirm + book seats +
  publish `booking.confirmed`; `payment.failed` → release the hold.
- **Seat double-booking under load** (P1-3) — added per-seat `xmin` optimistic
  concurrency; concurrent holds on one seat now yield exactly one 409.
- **IDOR** (P0-9, P0-10) — `CustomerId` + contact now come from the JWT, not the
  request body; `GET /bookings/{id}` and cancel return 404 for a non-owner.

### Added

- Endpoints: `GET /api/v1/bookings/mine`, admin `GET /api/v1/bookings`,
  admin `POST/GET /api/v1/trips`, public `GET /api/v1/trips/{id}` (live seat map).
- `ExpiredHoldSweepJob` (Quartz) — releases unpaid seat holds after the 10-min window.
- `inbox_messages` table — at-least-once consumer de-duplication.
- **Database provider factory** in booking (`Database:Provider` = Postgres | SqlServer | MySql).
- **File diagnostic logs** in booking: `logs/query-logs/` (structured: provider, endpoint,
  handler, correlation, SQL, timing, params, slow-query hints), `logs/runtime-errors/`
  (diagnosed root cause + fix), and `scripts/build-with-logs.sh` for `logs/build-errors/`.
- auth access token: `tenant_id`, `customer_id`, `phone_number` claims (additive).

### Changed

- `PaymentSucceededV1` / `PaymentFailedV1` contracts gained `CustomerId` + `OrderReference`;
  `BookingConfirmedV1` now carries the full journey + customer snapshot (for ticketing /
  notification). Payment domain events updated to match — additive, no break.
- booking middleware order: correlation → exception → auth.
- booking failure responses now use the unified envelope
  (`success/message/errors:[{code,field,message}]/traceId/timestamp`) — every validation
  error returned, not just the first.

### Operational notes

- No change to any other service's runtime behaviour. Payment/notification unaffected
  until they publish/consume the enriched events.
- New migration to apply: `dotnet ef database update` for booking-service (see MIGRATIONS
  section of `guide.md` / handover).
- Local `dotnet run`: always pass `Jwt__SigningKey` explicitly so auth and booking agree
  (compose already sets it).

### Known limitations

- booking IntegrationTests for the new consumer / job not yet written (unit-tested;
  end-to-end manually verified against real containers).
- MVP-2 (payment safety + QR), MVP-3 (ticketing), MVP-4 (notification) not started — see
  `ai-handover.md`.

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
