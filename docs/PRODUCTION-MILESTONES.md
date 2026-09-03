# Production Milestones — Bus Ticketing to Production

**Companion to** `PRODUCTION-GAP-ANALYSIS.md` (findings) and `API-GAPS.md` (endpoint map).
**Date:** 2026-08-31. **Audited commit:** `73081634`.

Each milestone is independently shippable, ends with a professional commit, and must not
regress the others. Order is fixed — later milestones depend on earlier ones. Every
milestone: build the affected services, run their xUnit unit + integration suites, add the
tests listed, update `docs/programmers-guide/`, commit.

Finding IDs (`P0-n`, `P1-n`, …) refer to `PRODUCTION-GAP-ANALYSIS.md`.

---

## M0 — Shared kernel + API gateway skeleton  *(P0-1, P0-2, P0-4, P2-3)*  — ✅ DONE (2026-09-01)

**Delivered:**
- `shared/shared-kernel` (`Platform.SharedKernel`) — `PlatformHeaders`, `CorrelationId`,
  `CorrelationContext` (AsyncLocal, replaces the racy `static` fields), `TenantContext` +
  `ITenantContextAccessor`, `Error`/`Result`/`Result<T>`, `ApiResponse<T>`,
  `PageRequest`/`PagedResult<T>`, `IdempotencyStore` contract, `RequestMetadata`.
- `shared/contracts` (`Platform.Contracts`) — `EventTypes` (all stable routing keys),
  `EventTypeRegistry` (40 domain events → keys), `IntegrationEventRoutingKeys` resolver
  (AQN/FullName/bare → key, deterministic no-double-prefix fallback), versioned
  `IntegrationEvents.*V1` contract records. **226 contract tests.**
- `shared/common` (`Platform.Common`) — canonical `CorrelationIdMiddleware`,
  `CorrelationPropagationHandler`, `SecurityHeadersMiddleware`, `TenantHeaderHygieneMiddleware`,
  `UsePlatformEdge()` / `UseTenantHeaderHygiene()`.
- **`infrastructure/gateway`** (`Platform.Gateway`) — YARP 2.3.0, 16 routes / 7 clusters
  (config-only), correlation ingress + YARP transform, tenant-header strip + claim
  re-inject, edge rate limiting (IP/user/tenant partitioned, 3 policies), security headers,
  10 MB body cap, per-cluster 30 s timeout, passive health checks, Serilog + OTel +
  `/metrics`, JWT-key-required-in-Production guard. Dockerfile (non-root `app`, HEALTHCHECK).
  **21 gateway tests.**
- **Routing-key fix (P0-4)** — all 6 `OutboxProcessor`s + payment's `FailedWebhookRetryJob`
  now use `IntegrationEventRoutingKeys.Resolve`. No `ToRoutingKey`/`DeriveRoutingKey` /
  AssemblyQualifiedName-as-routing-key left. **4 real-RabbitMQ integration tests** prove
  publish → `booking.confirmed` → bound queue receives it (and the old double-prefixed key
  does *not* match).
- **RabbitMQ correlation** — all 6 `RabbitMqPublisher`s set `IBasicProperties.CorrelationId`
  from `CorrelationContext.Current` when present.
- **Frontends** — Angular `proxy.conf.json` + `nginx.conf` + `environment*.ts` (+ new
  `environment.staging.ts` + `angular.json` staging config); React `vite.config.ts` +
  `nginx.conf` + `.env.{example,development,staging,production}` + `env.ts`. Every API call
  now goes to the single gateway; grep confirms zero direct service URLs/ports.
- **docker-compose** — `api-gateway` service (host `8088`), frontends `depends_on` it,
  their nginx targets it.
- Shared projects added to all 6 service `.sln`s; new `shared/Platform.Shared.sln` +
  `infrastructure/gateway/Platform.Gateway.sln`.
- Docs: `docs/programmers-guide/{gateway,messaging-contracts,correlation-id}.md`; `guide.md`,
  `ai-handover.md`, `docs/API-GAPS.md`, root `release-notes.md`.

**Verification:** all 6 service `.sln` build clean; **155/155 existing unit tests green**
(no regression); 226 + 21 + 4 = **251 new tests green**; gateway Docker image builds & runs
non-root with health 200; gateway→service routing + correlation preserve + `X-Tenant-Id`
strip verified end-to-end with the real image. The 2 pre-existing auth *integration* test
failures (`Admin_ListPermissions`, `SecurityQuestions_ConfigureAndVerify`) were confirmed
present on the clean baseline — **unrelated to M0**.

**Deferred (documented, tracked):** outbox `CorrelationId` column + full end-to-end message
correlation → M2/M9; Redis distributed rate limiter → M9; OTLP collector/Jaeger/Grafana →
M8; consumer inbox de-dup → M7; services adopting `shared/common` middleware & the shared
`CorrelationPropagationHandler` on outbound clients → M9/ongoing; per-service JWT keys →
M11; EF Core 9→10 for notification-service → M7.

---

### Original M0 plan (for reference)

**Goal:** one public ingress; one copy of every cross-cutting concern.

**Services touched:** `shared/*`, `infrastructure/gateway/*`, `infrastructure/docker/
docker-compose.yml`, both frontends' nginx / dev-proxy config. No business-logic service
code changes yet (migration onto the kernel happens milestone-by-milestone afterwards).

**Work:**
- Populate `shared/shared-kernel` (`Result`/`Error`, `ApiResponse<T>`, `ICurrentUser` +
  claims transformation, `CorrelationContext` as `AsyncLocal`, correlation
  `DelegatingHandler`), `shared/contracts` (event records + `EventType` constants + a single
  routing-key derivation), `shared/common` (exception middleware, outbox primitives,
  RabbitMQ publisher **with publisher confirms**, Redis idempotency + rate-limit helpers).
- New YARP project in `infrastructure/gateway/` — path-prefix routing to the 6 services,
  `UseForwardedHeaders` + trusted-proxy allowlist, correlation-id ingress, coarse IP rate
  limit. **No business logic.**
- Add `gateway` to `docker-compose.yml`; repoint `apps/*/nginx.conf`, `proxy.conf.json`,
  `vite.config.ts` at the gateway only.

**Acceptance:** both frontends work through a single base URL; no internal service port is
referenced by any frontend artefact; a request carries one correlation id gateway→service.

**Tests:** gateway route tests; forwarded-header strip test; `shared` unit tests.

**Rollback:** frontends fall back to the per-app nginx configs (kept in git history); the
gateway container can be removed from compose without touching services.

---

## M1 — Auth hardening  *(P0-12 partial, P1-19, P1-20, P1-21, P2-1)*

**Goal:** trustworthy identity — tenant + permission claims, real `ICurrentUser`, usable sessions.

**Services touched:** `services/auth-service/*`, both frontends' auth modules.

**Work:**
- `JwtTokenService.GenerateAccessToken` — add `tenant_id` (+ company/org where used) and a
  compact `perms` claim.
- Implement `ICurrentUser` (shared-kernel contract) from claims; wire claims transformation
  in each service's DI (used from M2 on).
- Document + script a production first-admin path; keep `DevAdminBootstrapper` non-prod only.
- Angular + React: 401-triggered refresh interceptor (single-flight, rotation-aware) against
  `/auth/refresh`; customer app: OTP request/verify screens against the existing endpoints.

**Acceptance:** a decoded access token carries `tenant_id` + `perms`; expired token →
silent refresh; OTP login works end-to-end.

**Tests:** token-claims unit test; refresh-interceptor test (both apps); OTP flow test.

**Rollback:** claims are additive; the SPAs degrade to the current re-login behaviour if the
interceptor is reverted.

---

## M2 — Booking correctness  *(P0-3, P0-4 booking, P0-9, P0-10, P1-1, P1-2, P1-3, P1-4)*

**Goal:** the reservation lifecycle is safe, owned, and completes.

**Services touched:** `services/booking-service/*`; `apps/*` (My Bookings wiring).

**Work:**
- Generate + commit the `InitialCreate` migration; in it add a unique constraint on
  `booking_seats (TripId, SeatNumber)` (P1-3).
- `Trip.HoldSeats` / `ConfirmSeats` — mark `Trip.Version` modified on any seat mutation.
- New `PaymentEventConsumer : BackgroundService` + inbox table: on `payment.succeeded` →
  `Booking.Confirm()` + `Trip.ConfirmSeats()` → emit `BookingConfirmed` (correct routing key
  from `shared/contracts`).
- New `ExpiredHoldSweepJob` (Quartz, persistent — see M9/P1-14; interim: single-instance).
- Remove `CustomerId` / `RequestedByCustomerId` from request DTOs; source from
  `ICurrentUser`. `GetBookingByIdHandler` — ownership check, 404 on mismatch, admin policy.
- New `GET /api/v1/bookings/mine` (paged, owner-scoped) + admin `GET /api/v1/bookings`
  (paged, filterable). Remove the frontend mock fallbacks for `bookings/mine`.
- Fix middleware order (correlation before exception handler).

**Acceptance:** fresh DB → `dotnet ef database update` creates the schema; create→pay→
confirm marks the booking `Confirmed` and seats `Booked`; an unpaid hold auto-cancels;
two parallel holds for one seat → one 409; `GET /bookings/{id}` of another user → 404;
`GET /bookings/mine` returns only the caller's bookings.

**Tests:** integration (consumer, expired-hold job); NBomber concurrency (0 double-books);
authz tests; `bookings/mine` pagination test.

**Rollback:** the consumer + job are additive; endpoints are new. The migration is the only
irreversible step — review before commit.

---

## M3 — Payment safety  *(P0-4 payment, P0-5, P0-6, P0-7, P0-11, P1-16, P1-17)*

**Goal:** no free payments, no forged webhooks, refunds that actually refund.

**Services touched:** `services/payment-service/*`.

**Work:**
- `ConfirmPayment` — never trust the request body; confirm only from a verified webhook or
  a server-side `provider.GetStatusAsync`. Add ownership + tenant checks.
- Webhooks — remove `DefaultPaymentProvider` fallback for signature; unknown provider or
  bad signature → 400 + log; guard the timestamp parse; add a webhook-event dedup table;
  `.RequireRateLimiting` on the route.
- Refunds — wire `IPaymentProvider.RefundAsync` into the handler or a `RefundProcessingJob`;
  drive `PaymentRefund` state from the provider result; `Payment.Status` follows confirmed
  refunds only; idempotent; amount ≤ captured.
- Register the circuit breaker / `ResilientPaymentProvider` in DI; one shared policy per
  provider; wrap execute/query/refund. Distinguish "unknown result" → pending-verification
  from "definite failure".
- Tenant isolation from claims (M1), not `X-Tenant-Id`; filter non-optional.
- Fix outbox routing-key derivation (use `shared/contracts`); fix middleware order
  (exception handler before auth/rate-limiter).

**Acceptance:** forged `/confirm` → rejected; forged webhook → 400; refund with provider
failure → `Payment` not `Refunded`; cross-tenant payment read → 404; provider timeout on
execute → pending-verification, not retried.

**Tests:** the security + refund + resilience tests listed in the gap analysis P0-5/6/7,
P1-17; webhook replay test.

**Rollback:** all changes tighten behaviour; revert per handler if a regression appears.

---

## M4 — Real bKash (sandbox → production-ready code)  *(P1-7)*

**Goal:** bKash tokenized-checkout that works against the real sandbox and is ready for
production credentials.

**Services touched:** `services/payment-service/src/PaymentService.Infrastructure/Providers/
BkashPaymentProvider.cs`, config, `docs/programmers-guide/`.

**Work:** obtain bKash sandbox app credentials; replace the fabricated webhook-HMAC check
with bKash's documented callback verification (re-query `payment/status` by `paymentID`);
wrap execute/query/refund in the M3 resilience policy; implement a real
`VerifyPaymentMethodAsync` or remove the fake and mark the capability unsupported; document
merchant onboarding, production config keys, and the go-live checklist. **No fake success,
no fake credentials.**

**Acceptance:** sandbox create→execute→query→refund succeeds; a tampered callback is
rejected; reconciliation job resolves a stuck `Processing` payment via the real query API.

**Tests:** sandbox integration test (CI, sandbox creds from secrets); tamper-rejection test.

**Rollback:** provider is config-selected; falls back to stub mode if creds are absent.

---

## M5 — Real Nagad (DFS envelope protocol)  *(P1-8)*

**Goal:** replace the invented Nagad protocol with the real DFS integration.

**Services touched:** `services/payment-service/.../Providers/NagadPaymentProvider.cs`,
config, `docs/programmers-guide/`.

**Work:** implement RSA sign/verify + AES-256 `sensitiveData`/`signature` envelopes against
the real endpoints (`/api/dfs/check-out/initialize/{merchantId}/{orderId}`,
`/complete/{paymentRefId}`, status/refund); config for the merchant private key + Nagad
public key; sandbox credentials; document key management + onboarding.

**Acceptance:** sandbox initialize→complete→verify succeeds; an envelope with a bad
signature is rejected.

**Tests:** sandbox integration test; signature-mismatch test; envelope encrypt/decrypt unit
tests.

**Rollback:** config-selected provider; absent creds → provider unavailable (explicit 503),
not fake success.

---

## M6 — Ticketing Service  *(P1-6, P1-9 groundwork)*

**Goal:** a real, verifiable, downloadable ticket.

**Services touched:** new `services/ticketing-service/*` (same conventions as the others);
`infrastructure/docker/docker-compose.yml`; `apps/angular-client/.../features/tickets`.

**Work:** new bounded context owning ticket issuance / ticket number / status / QR / PDF
(QuestPDF, template-based with operator assets — **not** cloned from an uploaded image) /
verification URL / templates / reissue / cancellation. Consumes `BookingConfirmed` → issues
a ticket → emits `TicketIssued`. Booking-service keeps reservation lifecycle only. Endpoints:
`GET /api/v1/tickets/mine`, `GET /api/v1/tickets/{id}`, `GET /api/v1/tickets/{id}/pdf`,
`GET /api/v1/tickets/verify/{code}` (public). Wire the customer ticket screens.

**Acceptance:** `BookingConfirmed` → ticket issued with a unique number; QR resolves to the
verify endpoint; PDF renders; reissue preserves the ticket number.

**Tests:** issuance on event; QR/verify; PDF render; reissue; unit tests for ticket-number
generation + state machine.

**Rollback:** new service — remove from compose to disable; booking/payment unaffected.

---

## M7 — Notification production-safe  *(P0-8, P1-10, P1-11, P1-12, P1-18)*

**Goal:** notifications that actually send, safely, at any replica count.

**Services touched:** `services/notification-service/*`.

**Work:** bump EF Core 9→10; seed the core templates (en + bn) via migration `HasData` /
idempotent seeder; a missing template → logged error + retry, not silent drop; implement one
Bangladesh SMS provider behind `ISmsSender` (real creds from secrets); add attachment / signed-
link support to `EmailMessage` + `SmtpEmailSender` for ticket PDFs; move idempotency to Redis
(M9 helper); make the dispatch job claim-then-send / save-per-item; `.RequireAuthorization()`
on send + history (or internal-only send behind the gateway); inbox dedup on the consumer
(`SourceReference` = unique event id).

**Acceptance:** after migrate+seed, publishing each event produces a rendered notification;
unauthenticated send/history → 401; kill mid-dispatch → no duplicate sends; two replicas →
each notification sent once.

**Tests:** template-render-on-event integration test; authz tests; crash-safety test;
duplicate-delivery → single send.

**Rollback:** EF bump + seeder are the irreversible parts — test first; auth + Redis changes
revert cleanly.

---

## M8 — Observability backend  *(P0-13, P1-16 verify)*

**Goal:** production is observable.

**Services touched:** `infrastructure/docker/docker-compose.yml`,
`infrastructure/monitoring/*`, minor `Program.cs` OTLP-config-key alignment.

**Work:** add an OTel Collector + Jaeger + Prometheus + Grafana (compose profile or the main
compose); fix `prometheus.yml` scrape targets (`/metrics` on 8080, all 6 services); align the
OTLP endpoint config key across services; verify RabbitMQ hops propagate trace context
(`traceparent` in `BasicProperties`); wire a log sink (Seq or Loki).

**Acceptance:** a booking+payment+notification flow is one connected trace in Jaeger;
Grafana shows per-service request/error/latency + booking/payment/notification business
metrics; `/metrics` scraped for all 6.

**Tests:** smoke — each `/metrics` returns Prometheus text; a synthetic trace appears in
Jaeger.

**Rollback:** pure infra addition; remove the profile to disable.

---

## M9 — Distributed rate limiting + resilience + outbox reliability  *(P1-13, P1-14, P1-15, P1-17)*

**Goal:** the platform behaves correctly with ≥2 replicas per service.

**Services touched:** all 6 (`Program.cs` + outbox + Quartz registration), `shared/common`,
`infrastructure/gateway`.

**Work:** Redis-backed distributed rate limiter in the shared kernel (policies by IP, user,
tenant, route); gateway does the coarse layer; booking gets limits. Quartz →
`UsePersistentStore` (Postgres) + `UseClustering` in auth/payment/notification/booking;
align Quartz version. Outbox → `FOR UPDATE SKIP LOCKED` (or a claim column) + publisher
confirms + a dead-letter table + metric/alert for exhausted rows + a cleanup job + a
RabbitMQ DLX. `AddStandardResilienceHandler` on every outbound typed client.

**Acceptance:** per-IP + per-user limits hold across 2 replicas; a Quartz job fires once
cluster-wide; 2 replicas publish each event once; a broker outage retains the outbox row;
an exhausted row lands in the DLQ with a metric.

**Tests:** 2-instance limiter test; 2-instance job-fires-once test; 2-instance outbox
no-double-publish test; DLQ test.

**Rollback:** limiter/resilience are config-gated; Quartz store change needs its tables
migrated — review first.

---

## M10 — SaaS foundation  *(P2-2, P2-3, P2-4)*

**Goal:** tenancy, subscription and localization ready for the later modules.

**Services touched:** `services/auth-service/*`, `shared/*`, all 6 (envelope + localization
roll-out), both frontends (i18n).

**Work:** implement ADR-0009 — `Subscription` / `Plan` / `PermissionLimit` /
`UserPermissionOverride` entities + migration in auth-service; `perms` claim emission
(from M1); a shared Redis-backed entitlement/limit library; 3-day trial + monthly plan
states; wire payment-service as the first consumer. Uniform `ApiResponse<T>` envelope +
all-errors validation across all services. One shared localization abstraction (en/bn,
extensible) + a versioned error-code → message-key catalogue; `@angular/localize` +
`react-i18next` in the SPAs.

**Acceptance:** a tenant on an expired subscription is denied write access; a per-day usage
limit is enforced across replicas; every API returns the standard envelope; UI switches
en/bn.

**Tests:** subscription-expiry test; usage-limit test; envelope contract test; localization
fallback-to-English test.

**Rollback:** entities are additive; the envelope + localization roll out per service —
revert per service if needed.

---

## M11 — CI/CD + production Docker  *(P0-12 finish, P1-22, P2-5, P2-7)*

**Goal:** repeatable, secure deployment.

**Services touched:** repo root (`global.json`, CI workflows), all Dockerfiles,
`docker-compose*.yml`, secrets config.

**Work:** `global.json` pinning the SDK; per-service JWT signing keys (or asymmetric) from
env/vault; scrub fallback secret literals; `throw` on missing key at startup; non-root user
+ `HEALTHCHECK` on the auth + route Dockerfiles; standardise base images + build contexts;
a production compose / Helm values with externalised secrets and TLS; CI pipeline per
service (restore→build→unit→integration→frontend tests→scan→docker build→publish);
staging + production deploy jobs; backup/restore runbook.

**Acceptance:** CI green on a clean checkout; a service with no signing key fails to start;
no secret literal in the repo; all 6 images non-root with a healthcheck.

**Tests:** the full CI suite; a secret-scanner step; container-structure test.

**Rollback:** CI + Docker changes don't affect running code; revert workflow files.

---

## Progress tracker

| Milestone | Status | Commit |
|-----------|--------|--------|
| M0 Shared kernel + gateway | ✅ Done (2026-09-01) | feat(platform): implement M0 shared kernel and YARP gateway |
| M1 Auth hardening | 🟡 Partial (2026-09-03) — `tenant_id` + `customer_id` + `phone_number` claims in the access token; claims-based `ICurrentUser` in booking-service. Remaining: `perms` claim, SPA refresh interceptor, OTP UI, production first-admin script. | feat(booking): M2 slice + auth claims |
| M2 Booking correctness | 🟡 Substantially done (2026-09-03) — `InitialCreate` migration; per-seat `xmin` concurrency; `PaymentEventConsumer` (payment.succeeded→confirm, payment.failed→release) + inbox dedup; `ExpiredHoldSweepJob` (Quartz); `GET /bookings/mine` + admin `GET /bookings`; ownership 404 on `GET /bookings/{id}` + cancel; `CustomerId`/contact from token not body; admin trip CRUD (`POST/GET /trips`, `GET /trips/{id}` seat map); DB-provider factory + file query/runtime logs. Remaining: booking IntegrationTests for the consumer + job; NBomber concurrency proof. | feat(booking): M2 — migrations, read-model, trip mgmt, payment-driven confirm |

| M3 Payment safety | 🟡 Substantially done (2026-09-03) — `ConfirmPayment` no longer trusts the request body (verifies via `provider.GetStatusAsync`, adds owner/tenant checks); `DefaultPaymentProvider` fails closed (webhook sig → false, confirm → Unknown, verify → Failed); unknown-provider / bad-sig webhook → 400; `RefundPaymentHandler` now calls `provider.RefundAsync` and drives payment/refund state from the result (P0-7); `Payment.Status` follows **settled** refunds only. Remaining: webhook-event dedup table, register `ResilientPaymentProvider` in DI, `CreatePayment` was hardened (tenant/customer from claims). | feat(payment): M3 + genuine EMVCo Bangla-QR |
| M4 Real bKash | Not started (bKash provider is HTTP-real but credential-gated; payload fields + real callback verification still to do) | |
| M5 Real Nagad | Not started (Nagad needs the real DFS RSA/AES envelope rewrite) | |
| — Genuine QR / Bangla QR | ✅ Done (2026-09-03) — `PaymentMethodType.Qr` + `QrPaymentProvider` (spec-correct EMVCo MPM payload, CRC-16, QRCoder PNG), `POST /payments/{id}/qr`, signed `POST /webhooks/qr` + audited admin `settle-qr`. Verified: customer→QR→settle→`payment.succeeded`→booking Confirmed. | feat(payment): M3 + genuine EMVCo Bangla-QR |
| M6 Ticketing Service | ✅ Done (2026-09-03) — new `services/ticketing-service` (Domain/App/Infra/Api + tests + .sln + Dockerfile). Consumes `booking.confirmed` (inbox dedup, idempotent) → issues a `Ticket` (checksummed number + opaque verification code) → renders an A5 **QuestPDF** with a QR to `/api/v1/tickets/verify/{code}` → emits `ticket.issued`. Endpoints: `/tickets/{mine,{id},{id}/pdf,verify/{code},cancel,reissue}` + `/ticket-templates` CRUD + logo upload. DB-provider factory, outbox, health, OTel, Scalar. Added to docker-compose (`postgres-ticketing`, `ticketing-service`), gateway `ticketing` cluster wired. **Verified:** book → QR pay → settle → booking Confirmed → ticket issued + 45 KB PDF + public verify + ownership 404. | feat(ticketing): M6 — new service: ticket issuance, QR, QuestPDF, templates |
| M7 Notification production-safe | Not started | |
| M8 Observability backend | Not started | |
| M9 Distributed limiting + resilience | Not started | |
| M10 SaaS foundation | Not started | |
| M11 CI/CD + production Docker | Not started | |

**Bus Ticketing production MVP = M0 → M8** (M4/M5 can run in parallel once M3 lands; M9 is
strongly recommended before real traffic). **SaaS foundation = M10.** **Deployable = M11.**
