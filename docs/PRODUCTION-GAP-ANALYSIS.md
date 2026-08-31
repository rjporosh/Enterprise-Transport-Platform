# Production Gap Analysis — Enterprise Transport Platform

**Audit date:** 2026-08-31
**Commit audited:** `73081634` (`main`)
**Scope:** Whole repository — actual source code, not documentation. Bus Ticketing is the
first production target; Train / Flight / Launch / Rickshaw / CNG / Van / Truck / Pickup and
the later SaaS products (Accounting, ERP, E-commerce, HRM, HMS, POS, Inventory) are future.
**Mode:** Audit only. No production source code was created, modified, renamed or deleted in
this pass. This document, `PRODUCTION-MILESTONES.md` and `API-GAPS.md` are the only outputs.

Every finding below was confirmed by reading the file cited. Classification:

| Tag | Meaning |
|-----|---------|
| **P0** | Production blocker — unsafe, insecure, or non-functional; must fix before any real traffic |
| **P1** | Required for the Bus Ticketing production MVP |
| **P2** | Multi-tenant SaaS / platform foundation — prepare now so later modules do not force a rewrite |
| **P3** | Future transportation / business-line expansion |

---

## A. Current architecture (as built, verified)

```
                 ┌─────────────────────────┐     ┌──────────────────────────┐
                 │ Angular 22 customer web  │     │ React 19 admin console   │
                 │ apps/angular-client/...  │     │ apps/react-admin/...     │
                 └────────────┬────────────┘     └────────────┬─────────────┘
                              │  (each app's own nginx / dev proxy — NO shared gateway)
        ┌──────────┬──────────┼───────────┬───────────┬───────────┐
        ▼          ▼          ▼           ▼           ▼           ▼
   auth-svc    booking-svc  bus-svc   route-svc   payment-svc  notification-svc
   :5203/8080  :8080        :5201     :5204       :5202        :8081
        │          │          │           │           │           │
     pg-auth   pg-booking  pg-bus     pg-route    pg-payment   pg-notification   (6 separate Postgres 16)
        └──────────┴──────────┴─── RabbitMQ topic exchanges `<svc>.events` ──────┘
                                    Redis (auth, bus, payment, route)
                                    MailHog (dev SMTP)
```

- **6 independent .NET 10 microservices**, each 4 projects (`*.Domain / *.Application /
  *.Infrastructure / *.Api`), Clean Architecture, MediatR + FluentValidation + EF Core all
  genuinely used, per-service Postgres schema (`auth`, `booking`, `bus`, `route`, `payment`,
  `notification`), transactional outbox → raw `RabbitMQ.Client` 6.8.1 (durable topic exchange
  `<svc>.events`, `Persistent=true`).
- **OpenAPI:** native `Microsoft.AspNetCore.OpenApi` + `Scalar.AspNetCore` at `/scalar` and
  `/openapi/v1.json` in all 6 (Swashbuckle deliberately not used). Version skew across
  services: `Scalar.AspNetCore` 2.1.2 / 2.2.0 / 2.9.0.
- **Messaging consumers:** only notification-service consumes events. Every other service is
  publish-only. No choreography/orchestration between the other five.
- **Background processing:** `OutboxProcessor` (`BackgroundService`) in all 6; Quartz in
  auth (1 job), payment (3 jobs), notification (2 jobs). Quartz uses the in-memory
  `RAMJobStore` (no persistence, no clustering).
- **Deployed infra** (`infrastructure/docker/docker-compose.yml`): Postgres ×6, RabbitMQ,
  Redis, MailHog, both frontends, the 6 API services. Nothing else.
- **gRPC:** `.proto` + `MapGrpcService` wired in bus, route, notification; present but
  commented out in auth; absent in booking and payment.
- **Tests:** per service `*.UnitTests` (handler/domain, EF InMemory + hand-rolled fakes) +
  `*.IntegrationTests` (usually a single `*ApiTests.cs`, Testcontainers — payment uses
  Sqlite instead). No messaging/outbox/consumer/contract tests. Load-test scaffolding
  (NBomber/k6/JMeter) exists unevenly. **Zero frontend tests.**

## B. All existing services / applications / components

| Component | Path | State |
|-----------|------|-------|
| auth-service | `services/auth-service` | Register/login/refresh(rotation+theft detection)/logout, lockout, OTP, audit, RBAC (User/Role/Permission/Module) + admin CRUD, outbox→RabbitMQ, Quartz OTP cleanup, DB-provider switch (PG/SqlServer/MySql), 5 migrations. **Runs as root, no Dockerfile HEALTHCHECK. No tenant/permission claims in JWT.** |
| booking-service | `services/booking-service` | Trip search, seat hold, create/cancel booking, outbox→RabbitMQ. **0 EF migrations. No rate limiting. No idempotency. No inbound consumer. No `ICurrentUser`. Npgsql hard-coded (no provider switch).** |
| bus-service | `services/bus-service` | Fleet/depot CRUD, bus lifecycle, tenant/soft-delete/audit fields, gRPC, Redis idempotency, file-based build/runtime/query logging, DB-provider switch, 7 migrations. |
| route-service | `services/route-service` | Routes/stops/schedules CRUD, soft delete, optimistic concurrency, audit interceptor, `.resx` localization (en/bn), gRPC, DB-provider switch, 3 migrations. **Global (non-partitioned) rate limiter. Runs as root, no HEALTHCHECK, `EXPOSE 80/443` but listens on 8080. `PollyPolicies.cs` is dead code.** |
| payment-service | `services/payment-service` | Payment lifecycle + state machine, provider abstraction + factory (Default/Bkash/Nagad/Stripe), create/process/confirm/fail/cancel/refund endpoints, webhook endpoint, Quartz reconciliation/webhook-retry/agent-verify jobs, outbox, Polly (partial), DB-provider switch incl. Sqlite, 5 migrations. **See §P (Payment) — multiple P0s.** |
| notification-service | `services/notification-service` | Email (MailKit SMTP), SMS (Twilio / GenericHttp), Push (FCM), Scriban templates, RabbitMQ consumer, Quartz dispatch + stuck-recovery, outbox, `.resx` localization. **Pinned to EF Core 9.0.0 on a net10 target. `POST /notifications` + history endpoints unauthenticated. In-memory idempotency + in-memory Quartz → not multi-instance safe. No template seeding.** |
| angular-client (customer web) | `apps/angular-client/bus-ticketing-customer-web` | Angular 22, standalone + signals. Login/register (password only), trip search, seat select, create booking, booking-confirmation page. **Payment page is a simulated card form. My Bookings + payment confirm served from in-app mock even in "real" mode. No token refresh, no i18n, no tests, no correlation-id header.** |
| react-admin (admin console) | `apps/react-admin/bus-ticketing-admin` | React 19, Vite 6, TanStack Query, axios. Auth (real), buses list (real), routes+stops (real), booking detail + cancel (real). **Dashboard stats, users, bookings-list, trips-list served from in-app mock. No token refresh, no i18n, no tests, no correlation-id header.** |
| shared kernel | `shared/common`, `shared/contracts`, `shared/shared-kernel` | **All three empty (only `.DS_Store`).** Everything is copy-pasted 6×. |
| API gateway | `infrastructure/gateway` | **Empty directory. No YARP, no Ocelot anywhere in the repo.** |
| Observability infra | `infrastructure/monitoring` | Only `prometheus/prometheus.yml`, and it scrapes one job at the wrong port. No Grafana/Jaeger/collector. Not in docker-compose. |
| Mobile | `apps/MAUI`, `apps/Flutter`, `apps/Native Android`, `apps/Native IOS` | Directories present; not audited this pass; not part of Bus Ticketing MVP. |

## C. Bus Ticketing — estimated completion

**≈ 35–40%.**

What is real: the 6 services scaffold and are mostly self-consistent; auth (register/login/
refresh/OTP/RBAC) is the most complete; trip search → seat hold → create booking happy-path
works in isolation; bus/route CRUD works; Scalar docs render; docker-compose brings the
stack up.

What is missing or unsafe for the ticketing MVP: the **pay → confirm booking → issue ticket
→ notify** spine is not connected end-to-end (booking never consumes `payment.succeeded`;
outbox routing keys don't match; no ticketing service exists); **payment confirm/refund/
webhook are unsafe** (client-trusted, no PSP call, signature-bypassable); **no real bKash,
no real Nagad, no Bangla QR**; **no authenticated "My Bookings"**; **booking endpoints are
IDOR / accept customer id from the body**; **booking has no DB schema (0 migrations)**;
**no API gateway**; **no PDF/QR ticket generation**; **no observability backend**.

## D. Overall SaaS — estimated completion

**≈ 15–20%.**

Identity + RBAC *shape* exists (User/Role/Permission/Module + admin CRUD). Everything else a
multi-tenant subscription SaaS needs is absent: no tenant/company/org claim in the JWT
(payment reads it from a spoofable header instead), no `Subscription`/`Plan`/`Entitlement`/
trial/feature-flag/usage-limit code (`docs/adr/0009` designs it — status *Proposed*), no
shared localization or stable error-code catalogue, no uniform API response envelope, no
gateway, no shared kernel, no deployed observability, no CI/CD.

---

## E. P0 findings — production blockers

| # | Service | File (verified) | Current implementation | Missing requirement | Production risk | Recommended solution | Dependencies | Tests required |
|---|---------|-----------------|------------------------|---------------------|-----------------|----------------------|--------------|----------------|
| P0-1 | platform | `infrastructure/gateway/` (empty); comments in `services/booking-service/src/BookingService.Api/Program.cs:91`, `services/auth-service/src/AuthService.Api/Program.cs:84`, `.../Security/ClientInfoExtensions.cs:5-7` | No gateway. Frontends hit each service through per-app nginx / dev proxy. Service code *assumes* a gateway validates JWTs and strips `X-Forwarded-For`. | One public ingress (YARP) doing routing, TLS termination, auth pre-check, correlation-id ingress, `UseForwardedHeaders` with a trusted-proxy allowlist, coarse rate limiting. Internal service URLs must never reach the browser. | Internal topology exposed; every cross-cutting concern re-implemented 6× and drifting; `X-Forwarded-For` spoofable (no proxy allowlist). | Add a YARP reverse-proxy project under `infrastructure/gateway/`, route `/api/v1/*` by path prefix, no business logic in it; point both frontends at the gateway base URL only. | M0 shared kernel | Gateway route tests; auth-forwarding test; header-strip test. |
| P0-2 | platform | `shared/common`, `shared/contracts`, `shared/shared-kernel` — only `.DS_Store` | Empty. Result/Error type, correlation + exception middleware, outbox, RabbitMQ publisher, base entities, **event contracts** all copied per service. | A referenced shared kernel: `Result`/`Error`, `ApiResponse<T>` envelope, correlation (AsyncLocal), exception middleware, outbox primitives, event-contract package, RabbitMQ publisher w/ confirms. | Drift already present: `Error` vs `ResultError`, static vs AsyncLocal correlation, three different idempotency stores, four routing-key derivations. Security fixes must be applied 6×. | Populate the three shared projects; migrate services onto them incrementally (one per milestone), keeping behaviour identical. | none | Unit tests move with the code; contract tests for the event package. |
| P0-3 | booking | `services/booking-service` has no `Migrations/`; `.../BookingService.Api/Program.cs:175-180` calls `db.Database.MigrateAsync()` | No migrations exist → `MigrateAsync` is a no-op → `booking` schema is never created. | `InitialCreate` migration committed; `booking_seats` unique `(TripId, SeatNumber)` constraint added at the same time (see P1-3). | Every booking DB call fails at runtime; the service is non-functional against a fresh database. | `dotnet ef migrations add "InitialCreate" --project services/booking-service/src/BookingService.Infrastructure --startup-project services/booking-service/src/BookingService.Api --output-dir Migrations`, review, commit. | none | Existing `BookingService.IntegrationTests` must run green against the generated schema. |
| P0-4 | booking, bus, route, payment | `services/booking-service/src/BookingService.Infrastructure/Persistence/Outbox/OutboxProcessor.cs:83-88`; equivalent in bus/route; `services/payment-service/.../Outbox/OutboxProcessor.cs` `DeriveRoutingKey` | `ToRoutingKey("BookingConfirmedDomainEvent")` → `"booking." + "booking.confirmed"` = **`booking.booking.confirmed`**. bus → `bus.bus.registered`, route → `route.route.created`. payment splits `AssemblyQualifiedName` → garbage. Consumer (`services/notification-service/.../Messaging/NotificationEventConsumer.cs:34-45` + `appsettings.json:33-40`) expects `booking.confirmed`, `payment.succeeded`. Only `auth.*` accidentally matches (event names are entity-prefixed). | Routing keys that match the published contract; a single shared derivation in the event-contract package. | Booking-confirmation and payment-receipt notifications **can never be delivered**. Any future consumer of bus/route/payment events is broken. | Fix the derivation (drop the extra prefix; for payment stop using `AssemblyQualifiedName`); ideally move to explicit `EventType` constants on the contract. | P0-2 (event contracts) | Consumer integration test: publish each event, assert the notification row is created. |
| P0-5 | payment | `services/payment-service/src/PaymentService.Application/Features/Payments/ConfirmPayment/ConfirmPaymentHandler.cs:25-50` | `ConfirmPayment` loads the payment and calls `payment.Succeed(request.ProviderTransactionId, request.ProviderReference)` using **client-supplied** values. No provider call, no signature verification, no ownership/tenant check. | Confirmation must be driven by a verified provider callback/query, not the client. Endpoint must check ownership. | Any authenticated caller can `POST /payments/{id}/process` then `POST /payments/{id}/confirm` and mark **any** payment `Succeeded` — free tickets. | Confirm only via (a) verified webhook or (b) server-side `provider.GetStatusAsync`; never trust the request body. Add ownership + tenant checks (see P0-11). | M3 | Test: `/confirm` with forged body → rejected; `/confirm` after a verified provider `Succeeded` status → accepted. |
| P0-6 | payment | `services/payment-service/src/PaymentService.Infrastructure/Providers/DefaultPaymentProvider.cs:83-86` (`return true;`); `.../PaymentProviderFactory.cs:50-51`; `.../Api/Endpoints/PaymentEndpoints.cs:165-194`; `.../ProcessWebhook/ProcessWebhookHandler.cs` | `POST /api/v1/webhooks/{providerName}` — no auth, no rate limit. Unknown `providerName` → factory returns `DefaultPaymentProvider` whose `VerifyWebhookSignature` is `return true;`. `DateTimeOffset.Parse(timestampHeader)` unguarded. | Per-provider signature verification is mandatory; no default-allow; unknown provider → reject; webhook route rate-limited; timestamp parse guarded; replay/dedup by provider event id. | Attacker `POST /api/v1/webhooks/anything` with a crafted body → accepted → drives a payment to `Succeeded`. Malformed timestamp → 500. | Remove the default-allow; if provider unknown or signature invalid → 400 and log. Add `IdempotencyKey`/event-id dedup table for webhooks. Rate-limit the route. | M3 | Forged webhook (unknown provider) → 401/400; replay of a valid webhook → single effect; malformed timestamp → 400 not 500. |
| P0-7 | payment | `services/payment-service/src/PaymentService.Application/Features/Payments/RefundPayment/RefundPaymentHandler.cs:24-46`; `.../Domain/Entities/Payment.cs:167-199`; `.../Domain/Entities/PaymentRefund.cs:41-73` | `RefundPayment` calls only `payment.InitiateRefund(...)`, which immediately flips `Payment.Status` to `Refunded`/`PartiallyRefunded` and creates a `PaymentRefund` in status `Pending`. `IPaymentProvider.RefundAsync` (implemented for Bkash/Nagad/Stripe) is **never invoked anywhere**; nothing processes pending refunds. | Refund must call the PSP, track `PaymentRefund` through `Processing`→`Succeeded`/`Failed`, and only then update ledger state; idempotent; refund amount ≤ captured. | Payment ledger says "refunded" but **no money is returned**; reconciliation diverges permanently. | Wire `RefundAsync` into the handler or a `RefundProcessingJob`; drive `PaymentRefund` state from the provider result; keep `Payment.Status` in step with confirmed refunds only. | M3 | Refund happy-path (provider success), provider failure (refund stays `Failed`, payment not `Refunded`), duplicate refund request → single PSP call. |
| P0-8 | notification | `services/notification-service/src/NotificationService.Api/Endpoints/NotificationsEndpoints.cs:16-34` (send + `GET /{id}` + `GET /` have **no** `.RequireAuthorization()`; only `retry`/`delete` do, lines 43-55); `.../Api/Program.cs:52-60` | `POST /api/v1/notifications` (send) and both read endpoints are unauthenticated (IP rate limit only, 60/min). | Auth on send and on history; send restricted to service-to-service creds or an internal-only network path behind the gateway. | Anyone who can reach the service sends arbitrary email/SMS/push to any recipient and reads all notification history (recipient emails/phones + message bodies). | `.RequireAuthorization()` on send + history with a service/scope policy; or make send internal-only (gateway does not expose it) and require auth on history. | P0-1, M7 | Unauthenticated send → 401; unauthenticated history → 401; service-token send → 202. |
| P0-9 | booking | `services/booking-service/src/BookingService.Application/Features/Bookings/GetBookingById/GetBookingByIdHandler.cs:15-35`; `.../Endpoints/BookingsEndpoints.cs:40` | Group has `.RequireAuthorization()` but `GetBookingByIdHandler` has no `ICurrentUser` and no ownership check — returns the booking (incl. passenger name/age/gender) to any authenticated user. `ICurrentUser` interface exists with **zero implementations, zero usages**. | Implement `ICurrentUser` (from JWT claims) and enforce `booking.CustomerId == currentUser.CustomerId` (or an admin/operator policy). | Authenticated IDOR — enumerate booking GUIDs, harvest passenger PII. | Implement `CurrentUser : ICurrentUser` in the Api layer; inject into the handler; 404 (not 403) on ownership mismatch. | M1 (claims), M2 | Owner reads own booking → 200; non-owner → 404; admin policy → 200. |
| P0-10 | booking | `services/booking-service/src/BookingService.Application/Features/Bookings/CreateBooking/CreateBookingCommand.cs:12-15`; `.../CancelBooking/CancelBookingHandler.cs:43`; `.../Endpoints/BookingsEndpoints.cs:50-58` | `CreateBookingCommand.CustomerId` and `CancelBookingRequest.RequestedByCustomerId` are bound from the request body. Cancel checks `booking.CustomerId != RequestedByCustomerId` — but the caller supplies that value, and `GetBookingById` leaks the real `CustomerId` (P0-9), so the check is trivially bypassed. | Customer id must come from `ICurrentUser`, never the body. | Create bookings as any customer; cancel anyone's booking (releasing their held seats). | Remove `CustomerId`/`RequestedByCustomerId` from the request DTOs; source from claims. | M1, M2 | Create ignores body customer id; cancel of another user's booking → 404. |
| P0-11 | payment | `services/payment-service/src/PaymentService.Api/Middleware/RequestContextMiddleware.cs:17-30`; `.../Api/Security/CurrentUser.cs:29-53`; `.../Features/Payments/GetPaymentById/GetPaymentByIdHandler.cs:36` | Tenant/company/org come from **request headers** `X-Tenant-Id` etc. copied into `HttpContext.Items`. `GetPaymentByIdHandler` enforces tenant **only if the header is present**; omit it → any payment readable by GUID. List/search filter on a query-param `TenantId`. | Tenant context from JWT claims (or a trusted server-side resolver), never client headers; enforced on every read/mutation unconditionally. | Cross-tenant payment disclosure (IDOR); contradicts MASTER-RULES §25. | Add tenant/company/org claims to the JWT (M1); resolve tenant from `ICurrentUser`; make the filter non-optional; ignore inbound `X-Tenant-Id` from non-gateway callers. | M1 | No-header read of another tenant's payment → 404; cross-tenant list → empty. |
| P0-12 | platform | `infrastructure/docker/docker-compose.yml` (`Jwt__SigningKey` identical on lines 71,165,210,257,308,360; `POSTGRES_PASSWORD: changeme` ×6); `services/payment-service/.../Program.cs:45`, `services/booking-service/.../Program.cs:106` (fallback keys); `services/notification-service/.../appsettings.json:24` (`""`) | One shared HMAC signing key for all services; code fallbacks let a service boot and validate JWTs with no key configured; notification's configured key is an empty string. | Per-service asymmetric keys (or per-service secrets from a vault); fail-fast if no key; no fallback secrets in code. | Any service (or anyone who reads the compose file) can mint tokens every other service accepts. Empty key = trivially forgeable tokens. | Move secrets to env/vault; distinct keys; `throw` on missing key at startup; scrub the fallback literals. | M11 | Startup with no key → fail fast; token signed by service A rejected by service B (once keys differ / audience-scoped). |
| P0-13 | platform | `infrastructure/monitoring/prometheus/prometheus.yml` (one job, `host.docker.internal:5000`); all 6 `Program.cs` OTLP exporters → `localhost:4317`; docker-compose has no collector/Jaeger/Grafana | OTel SDK is wired in every service but exports to a dead endpoint; Prometheus config scrapes one service at the wrong port; no trace/metrics/log backend deployed. | Deploy an OTel Collector + Jaeger + Prometheus (correct scrape targets, `/metrics` on 8080) + Grafana; wire log shipping. | No traces, no metrics, no dashboards, no alerting — production is unobservable; silent exporter retry churn. | Add the observability stack to compose (or a separate compose profile); fix `prometheus.yml` targets; standardise the OTLP endpoint config key (`payment` uses `OpenTelemetry:Endpoint`, others `OpenTelemetry:OtlpEndpoint`). | M8 | `/metrics` reachable per service; a trace spanning gateway→service→RabbitMQ visible in Jaeger. |

## F. P1 findings — required for the Bus Ticketing MVP

| # | Service | File (verified) | Current | Missing | Risk | Recommended solution | Tests |
|---|---------|-----------------|---------|---------|------|----------------------|-------|
| P1-1 | booking | booking-service has no consumer class; `.../Domain/Entities/Booking.cs:75` `Confirm()` is never called | booking publishes `BookingCreated/Confirmed/Cancelled` but consumes nothing. | An inbound RabbitMQ consumer that, on `payment.succeeded`, loads the booking, calls `Booking.Confirm()` + `Trip.ConfirmSeats()`, saves, emits `BookingConfirmed`. Inbox dedup. | Bookings stay `PendingPayment` and seats stay `Held` forever; the core flow never completes. | Add `PaymentEventConsumer : BackgroundService` (mirror notification's consumer) + inbox table. | Consume `payment.succeeded` → booking `Confirmed`, seats `Booked`; duplicate delivery → single confirm. |
| P1-2 | booking | `.../Domain/Entities/Booking.cs:96` `IsHoldExpired` never called; no Quartz in booking-service | Held seats are never released if payment doesn't happen. | Quartz job: find `PendingPayment` bookings past `HoldExpiresAtUtc`, cancel them, release seats, emit `BookingCancelled`. `HoldSeats` should also treat an expired hold as available. | Inventory permanently leaks; trips sell out with unpaid holds. | Add `ExpiredHoldSweepJob` (persistent, clustered Quartz — see P1-14). | Hold older than 10 min → auto-cancelled, seat back to `Available`. |
| P1-3 | booking | `.../Domain/Entities/Trip.cs:59-73` `HoldSeats`; `.../Persistence/Configurations/TripSeatConfiguration.cs`, `BookingSeatConfiguration.cs`; `TripConfiguration.cs:26-33` (`xmin` on `Trip` only) | `HoldSeats` mutates only child `TripSeat` rows; parent `Trip.Version` is never marked modified; `TripSeat` has no concurrency token; `booking_seats` has no unique `(TripId, SeatNumber)`. Under READ COMMITTED two concurrent holds for the same seat both commit, no `DbUpdateConcurrencyException`. | Either a unique constraint on `booking_seats (TripId, SeatNumber)` (simplest, DB-enforced) or `Entry(trip).Property(x => x.Version).IsModified = true` in `HoldSeats`, or a pessimistic `SELECT ... FOR UPDATE` on the trip row, or a Redis seat lock. | Double-booked seats; two passengers, one seat. | Add the unique constraint in the `InitialCreate` migration (P0-3) **and** touch `Trip.Version` on any seat mutation. | The existing NBomber concurrency test under `services/booking-service/performance-tests/nbomber/` must show 0 double-books; xUnit test with 2 parallel holds → one 409. |
| P1-4 | booking / frontend | booking-service endpoints (`BookingsEndpoints.cs`) have no list route; `apps/angular-client/.../core/interceptors/mock-api.interceptor.ts` and `apps/react-admin/.../api/mockAdapter.ts` serve `GET /bookings/mine` from fixtures | No authenticated "My Bookings". | `GET /api/v1/bookings/mine` (paged, `ICurrentUser`-scoped) + admin `GET /api/v1/bookings` (paged, filterable). Then remove the frontend mock fallbacks. | Customers can't see their tickets; admin can't list bookings; "real API" mode is partly fake. | Add the two query handlers + endpoints; wire both frontends. | Owner sees only own bookings, paged; admin list filters by status. |
| P1-5 | payment / auth / frontend | `apps/angular-client/.../features/payment/pages/payment-page`; `.../core/interceptors/mock-api.interceptor.ts` (`POST /payments/{id}/confirm` mock); auth `JwtTokenService` issues no tenant claim; payment `CreatePaymentCommand` requires `TenantId` | Customer payment page is a simulated card form calling a mock-only confirm. Real payment-service is B2B-shaped (needs `TenantId`), customer JWT has none. | Decide the retail flow: a default/"retail" tenant for B2C, or a tenant claim for every user. Wire the Angular payment page to real `create` → provider redirect → webhook confirm. | No real payments possible from the customer app. | M1 adds tenant claim; M3 makes confirm safe; then wire the SPA to the real create + hosted-checkout redirect. | End-to-end: create booking → create payment → sandbox provider → webhook → booking confirmed. |
| P1-6 | ticketing (new) | no ticketing service in `services/`; no PDF lib (QuestPDF/iText/PdfSharp) anywhere; `apps/angular-client/.../features/tickets` + `.../state/ticket` have no backend | Nothing issues a ticket: no ticket number, status, QR/barcode, verification URL, PDF, template, reissue. | A dedicated **Ticketing Service** owning ticket issuance/number/status/QR/PDF/verification/templates/reissue/cancellation. Consumes `BookingConfirmed` → issues ticket → emits `TicketIssued`. Booking keeps reservation lifecycle only. | No deliverable ticket → the product does not function. | New bounded context (`services/ticketing-service`), same conventions as the others; template-based PDF (QuestPDF), do **not** clone an uploaded ticket image — use a template with the operator's assets. | Issue on `BookingConfirmed`; QR verifies; PDF renders; reissue keeps the ticket number. |
| P1-7 | payment | `services/payment-service/src/PaymentService.Infrastructure/Providers/BkashPaymentProvider.cs` (`:41-48` stub gate, `:325-345` fabricated webhook HMAC, `:347-370` fake `VerifyPaymentMethodAsync`, `:133-177` execute not retry-wrapped); `appsettings.json:39-48` (blank creds) | Real token-grant + create/execute/query/refund HTTP against tokenized-checkout URLs, but stub-gated on blank `AppKey`/`AppSecret`, invented webhook signature scheme, fake account verification. | Sandbox app credentials; bKash's real callback/IPN verification (query-based, not shared-HMAC); wrap execute/query/refund in the resilience policy; merchant onboarding + production config. | "bKash integration exists" is misleading — it will not work in production as-is. | Get bKash sandbox creds; replace the fabricated webhook check with bKash's documented callback verification (re-query `payment/status` by `paymentID`); document onboarding. Keep code abstraction. | Sandbox: create→execute→query→refund; callback verification rejects a tampered callback. |
| P1-8 | payment | `services/payment-service/src/PaymentService.Infrastructure/Providers/NagadPaymentProvider.cs:284-321` | The Nagad "session/create with `{merchantId, secretKey}`" protocol is **invented**. Real Nagad DFS is RSA keypair + AES `sensitiveData`/`signature` envelopes against `/api/dfs/check-out/initialize/{merchantId}/{orderId}` and `/complete/{paymentRefId}`. No RSA/AES/key config exists. | A ground-up Nagad DFS implementation (RSA sign/verify, AES-256 encrypt/decrypt, the real endpoints), config for merchant private key + Nagad public key, sandbox creds, onboarding docs. | Non-functional against real Nagad; effectively a rewrite. | Implement the real DFS envelope protocol behind the existing `IPaymentProvider`; document key management. | Sandbox: initialize→complete→verify; envelope signature mismatch → rejected. |
| P1-9 | payment | (feature absent) | No Bangla QR / EMVCo QR / Nagad QR anywhere. | If QR acceptance is in scope: an EMVCo-compliant merchant/consumer-presented QR built through an acquirer (bank/PSP). Requires acquirer onboarding, merchant ID, settlement/reconciliation. | Cannot accept interoperable QR payments (a common BD retail method). | Treat as a distinct provider integration via `IPaymentProvider` once an acquirer is chosen; do **not** invent a QR protocol. Document acquirer/merchant/verification/reconciliation requirements. | Sandbox QR generate + acquirer callback + reconciliation. |
| P1-10 | notification | no `HasData` in `services/notification-service/.../Migrations/`; no seeder in `Program.cs`/DI; `.../Features/.../SendNotificationHandler.cs:69-87` returns `Error.NotFound` when a template is missing; consumer acks+drops | Zero templates out of the box → every event notification silently fails. | Seed the core templates (`auth.welcome`, `booking.held`, `booking.confirmed`, `booking.cancelled`, `payment.receipt`, `payment.failed`, plus ticket templates) in en + bn via migration `HasData` or an idempotent startup seeder. | Notifications appear "wired" but never send. | Add a seeder; make a missing template a logged error + retry, not a silent drop. | After migrate+seed, publishing each event produces a rendered notification. |
| P1-11 | notification | `services/notification-service/src/NotificationService.Infrastructure/Channels/Sms/` (only `TwilioSmsSender`, `GenericHttpSmsSender`); `appsettings.json:60-67` blank | No Bangladesh SMS provider; `GenericHttp` payload shape ≠ SSL Wireless. | An `ISmsSender` implementation for a real BD provider (SSL Wireless / Robi / Banglalink / GP / a bulk aggregator) with the correct request contract and delivery-report handling. | Cannot send ticket/OTP SMS in Bangladesh. | Implement one BD provider behind `ISmsSender`; keep provider choice in `Sms:Provider`. Document credential acquisition (no fake creds). | Sandbox/live-test send + delivery report parsed. |
| P1-12 | notification | `services/notification-service/src/NotificationService.Infrastructure/Channels/Email/SmtpEmailSender.cs`; `EmailMessage` has only `Subject`/`HtmlBody`/`PlainTextBody` | No attachment support → cannot attach a PDF ticket. | Add attachment (or a signed download link) support to `EmailMessage` + `SmtpEmailSender`; ticket emails carry the PDF or a link. | Ticket emails can't include the ticket. | Extend the model; MailKit supports attachments directly. Prefer a signed link for large PDFs. | Booking-confirmed email carries a working ticket link/attachment. |
| P1-13 | platform | rate limiters: `services/auth-service/.../Program.cs:86-98`, `bus/.../Program.cs:76-109`, `notification/.../Program.cs:85-101`, `payment/.../Program.cs:63-76`, `route/.../Program.cs:62-70`; booking none | All in-memory `FixedWindow`; route uses one global bucket; payment partitions by `Host` header when unauthenticated; booking has none; `X-Forwarded-For` trusted without a proxy allowlist. | Redis-backed distributed limiter; policies by IP, authenticated user, tenant, and route; strict limits on login/OTP/password-reset/payment/webhook; coarse limit at the gateway. | Limits don't hold across replicas; route limiter starves all callers; booking is unprotected; IP spoofable. | Shared Redis limiter in the shared kernel; per-dimension policies; gateway does the coarse layer. | Per-IP and per-user limits enforced across 2 replicas; login brute-force blocked. |
| P1-14 | auth, payment, notification | `services/*/src/*.Infrastructure/.../QuartzRegistration.cs` / `DependencyInjection.cs` — no `UsePersistentStore`, no clustering | Quartz `RAMJobStore`: every replica runs every job. | `UsePersistentStore` (Postgres) + `UseClustering` so a job fires once cluster-wide. Align Quartz version (3.13.0/3.13.1/3.14.0). | Duplicate reconciliation / cleanup / dispatch runs under scale-out (double emails, double refund queries). | Configure the ADO job store + clustering; add the Quartz tables via migration. | 2 instances, a job fires once; `[DisallowConcurrentExecution]` respected cluster-wide. |
| P1-15 | platform | all 6 `.../Persistence/Outbox/OutboxProcessor.cs` | Poll `WHERE ProcessedOnUtc IS NULL` `Take(n)`, no row claim; rows at `RetryCount == 5` abandoned silently; no DLQ; no publisher confirms; no processed-row cleanup. | `FOR UPDATE SKIP LOCKED` (or a claim column); publisher confirms (`ConfirmSelect`/`WaitForConfirms`); a dead-letter table + metric + alert for exhausted rows; a cleanup/archival job; RabbitMQ DLX. | Multi-instance double-publish; silently lost events; unroutable messages dropped while marked processed. | Add row claiming + confirms + DLQ to the shared outbox. | 2 instances publish each event once; broker-down → row stays unprocessed; exhausted row → DLQ + metric. |
| P1-16 | platform | no `DelegatingHandler` on any HttpClient; no RabbitMQ publisher sets `IBasicProperties.CorrelationId`; booking `OutboxMessage` has no `CorrelationId` column; payment/route have it but never populate it; `services/booking-service/.../Program.cs:149-150` (exception mw before correlation mw); `services/payment-service/.../Program.cs:177-181` (exception mw after auth/rate-limiter) | Correlation id is created at ingress but not propagated to outbound HTTP, RabbitMQ, or Quartz jobs; two services have middleware-ordering bugs. | A correlation `DelegatingHandler` on every typed client; set `BasicProperties.CorrelationId` + a header on publish and read it on consume; carry it through the outbox row; scope it in Quartz jobs; fix middleware order. | A payment can't be traced client→service→provider→RabbitMQ→notification; unhandled-exception logs in booking lack the correlation id. | Shared handler + publisher + consumer in the shared kernel; reorder middleware. | A single correlation id visible across an end-to-end booking+payment+notification flow in logs. |
| P1-17 | platform | route `.../Communication/PollyPolicies.cs` (unreferenced); payment `.../Providers/ResilientPaymentProvider.cs` + circuit breaker (never registered); `.../Providers/*PaymentProvider.cs` build a new Polly policy per ctor; auth/booking/bus have no outbound resilience | Resilience is dead code or per-instance (breaker state can't accumulate) or absent. | `AddStandardResilienceHandler` (Polly v8 pipeline) on every outbound typed client; a single shared circuit breaker per provider; never blind-retry a non-idempotent payment execute. | Provider outage cascades; retry storms; or (for payment) a double charge on a naive retry. | Register the resilience handler once per client; for payment distinguish "unknown result" from "definite failure" and reconcile rather than retry. | Provider 500 → retried with backoff; provider timeout on execute → payment goes to a pending-verification state, not retried. |
| P1-18 | notification | `services/notification-service/src/NotificationService.*/*.csproj` (EF Core `9.0.0`); `.../Api/Middleware/IdempotencyMiddleware.cs:24` (in-memory dict); `.../Scheduling/Jobs/NotificationDispatchJob.cs:68-77` (batch-send then one save) | Pinned to EF Core 9 on a net10 target; in-memory idempotency; dispatch job sends all 50 then saves once (crash mid-batch → re-send). | Bump to EF Core 10; Redis idempotency (P1-13); save-per-item or claim-then-send in the dispatch job. | Version drift risk; duplicate sends on crash or scale-out. | Bump + retest; move idempotency to Redis; make dispatch crash-safe. | Kill the service mid-batch → no duplicate sends on restart. |
| P1-19 | frontend | `apps/angular-client/.../core/auth/*`, `apps/react-admin/.../modules/auth/*` — `refresh_token` stored, never used; auth-service `/auth/refresh` exists | Neither SPA refreshes tokens; a 15-min access token expiry logs the user out. | An HTTP interceptor that refreshes on 401 (once, with a queue) using the stored refresh token; rotation-aware. | Users bounced to login every 15 minutes. | Add the refresh interceptor in both apps against the real `/auth/refresh`. | Expired access token → silent refresh → request retried; refresh failure → clean logout. |
| P1-20 | frontend | `apps/angular-client/.../features/auth/*` — password login only; auth-service `/auth/otp/request`, `/auth/otp/verify` exist with en/bn resources | OTP backend unused by the customer app. | OTP request/verify UI in the customer login flow. | A built, localized security feature is unreachable. | Add the OTP screens against the existing endpoints. | Request OTP → verify → session established; wrong OTP × N → lockout message. |
| P1-21 | auth / ops | `apps/react-admin/.../ai-handover.md` documents the manual SQL promote; `DevAdminBootstrapper` is env-gated, disabled by default | No seeded admin; first admin is a manual `INSERT` into `auth.user_roles`. | A documented, safe bootstrap path (one-time env-gated bootstrap that self-disables, or a migration seed for a non-prod default admin only). | Cannot administer a fresh deployment without DB access. | Keep `DevAdminBootstrapper` for non-prod; document a production first-admin procedure (CLI/one-shot job). | Fresh stack + bootstrap flag → working admin login; flag off in prod → no default admin. |
| P1-22 | frontend | no `*.spec.ts` in either app; test tooling configured (Karma/Jasmine; none in React) | Zero frontend tests. | Component/service tests for the critical flows (auth, search, booking, payment, error handling, guards). MASTER_SPEC "Definition of Done" requires tests per feature. | Regressions ship silently. | Add a minimal test suite per app; wire into CI (M11). | Critical-flow tests green in CI. |

## G. P2 findings — SaaS / platform foundation

| # | Area | File (verified) | Current | Missing | Recommended solution |
|---|------|-----------------|---------|---------|----------------------|
| P2-1 | Tenancy / identity | auth `JwtTokenService.GenerateAccessToken` (claims: `sub`, `email`, `jti`, `iat`, `first_name`, `last_name`, `role` only) | No tenant/company/org/permission/subscription claim. Payment reads tenant from a spoofable header (P0-11). | Add `tenant_id` (+ company/org where used) and a compact `perms` claim to the JWT; every service resolves tenant from claims. | M1: extend `JwtTokenService`; add a claims-transformation + `ICurrentUser` in the shared kernel. |
| P2-2 | Subscription / entitlement | `docs/adr/0009-subscription-licensing-and-module-rate-limits.md` (status **Proposed**) | No `Subscription`/`Plan`/`PermissionLimit`/`UserPermissionOverride` entities; no trial, no feature flag, no usage limit in code. | Implement ADR-0009 in auth-service + a shared Redis-backed entitlement/limit library; 3-day trial + monthly plan states. | M10 — do the entities + claim emission + shared library first, wire payment as the first consumer. |
| P2-3 | API response envelope | per-service `Common/Models/Result.cs` + `Error.cs`/`ResultError.cs` (6 copies, names differ) | Result pattern exists but the wire envelope (`success/message/data/errors[]/traceId/timestamp`) isn't uniformly enforced; validation doesn't always return all errors. | One `ApiResponse<T>` + an endpoint filter / result-mapping in the shared kernel; FluentValidation configured to collect all failures. | M0 (shared kernel) then roll out per service. |
| P2-4 | Localization | auth inline dictionary; bus JSON files; route + notification `.resx`; booking + payment none; bare `bn` (no `bn-BD`) | 4 bespoke `ILocalizationService`; 2 services have none; no stable error-code catalogue; frontends have no i18n at all. | One shared localization abstraction + resource convention (en/bn, extensible); a versioned error-code → message-key catalogue; `@angular/localize` + `react-i18next` in the SPAs. | M10. |
| P2-5 | SDK pinning | no `global.json` in the repo; `docs/adr/0001-use-dotnet-10.md` | .NET SDK version unpinned. | Add `global.json` pinning the SDK band. | M11. |
| P2-6 | Docs | `docs/` — 85 of 98 `.md` are <5-byte stubs; no root `docs/programmers-guide/`; `.ai/communication.md` empty (mandatory reading per MASTER-RULES §2); `docs/.ai/AI_RULES.md` conflicts with `.ai/AI_RULES.md` | Most promised docs are placeholders. | Fill the programmer-guide set as each milestone lands; write `.ai/communication.md`; delete or reconcile the duplicate `AI_RULES.md`. | Ongoing, per milestone. |
| P2-7 | CI/CD | no `infrastructure/cicd`, no `.github/workflows` present | No pipeline. | Restore→build→test→scan→docker-build→publish per service; run the frontend + integration tests. | M11. |
| P2-8 | Load balancing | (none) | No LB config; gateway absent. | Once the gateway exists, run ≥2 replicas per service behind it; document the LB tier. | M9. |
| P2-9 | Polyglot readiness | `.ai/backend/{java-spring,node-express,node-nextjs,python-fastapi}.md` exist; no such services | Future Node/Java/Python services have rules but no integration contract to build against. | Publish the shared JSON Schema / OpenAPI / AsyncAPI contracts + the correlation/tenant/idempotency/auth conventions as language-neutral artefacts once the shared kernel is real. | After M0 + M10. |

## H. P3 findings — future expansion

| # | Area | Note |
|---|------|------|
| P3-1 | Other transport modes (Train, Flight, Launch/Ferry, Rickshaw, CNG, Van, Truck, Pickup) | None exist. The per-service-schema + event-bus architecture can host them once the shared kernel (P0-2), gateway (P0-1) and Ticketing boundary (P1-6) exist. Each mode is a bounded context reusing Ticketing + Payment + Notification. |
| P3-2 | Other SaaS products (Accounting, ERP, E-commerce, HRM, HMS, POS, Inventory) | Out of scope. Only the auth + subscription/entitlement foundation (P2-1, P2-2) needs to be built so as not to block them. |
| P3-3 | AI-assisted ticket-template extraction from an uploaded ticket image | A separate future service. For now, template-based ticket design with operator-supplied assets only (P1-6). Do not clone uploaded ticket images. |
| P3-4 | Mobile clients (`apps/MAUI`, `apps/Flutter`, native) | Not audited; not on the Bus Ticketing critical path. They will consume the same gateway + contracts. |

---

## I. Exact implementation order

`M0 → M1 → M2 → M3 → M4 → M5 → M6 → M7 → M8 → M9 → M10 → M11` (detailed in
`PRODUCTION-MILESTONES.md`). Rationale: the shared kernel + gateway (M0) unblock every
later security fix (apply once, not 6×); auth claims + `ICurrentUser` (M1) are a prerequisite
for the booking (M2) and payment (M3) ownership fixes; the booking/payment spine (M2, M3)
must be safe before real PSPs (M4, M5); Ticketing (M6) needs a confirmed booking to exist;
Notification hardening (M7) needs the routing keys fixed (M0) and Ticketing (M6) for ticket
messages; observability (M8), distributed limiting/resilience (M9), SaaS foundation (M10)
and CI/CD + prod Docker (M11) then layer on.

## J. Files / services to touch in Phase 1 (M0 + M1)

**M0 — shared kernel + gateway**
- `shared/shared-kernel/*` — new: `Result`/`Error`, `ApiResponse<T>`, `ICurrentUser` +
  claims transformation, `CorrelationContext` (AsyncLocal), correlation `DelegatingHandler`.
- `shared/contracts/*` — new: event-contract records + `EventType` constants + one
  routing-key derivation.
- `shared/common/*` — new: exception middleware, outbox primitives, RabbitMQ publisher
  (with confirms), Redis idempotency + rate-limit helpers.
- `infrastructure/gateway/*` — new YARP project; `infrastructure/docker/docker-compose.yml`
  — add the `gateway` service; `apps/angular-client/.../nginx.conf`,
  `apps/react-admin/.../nginx.conf`, `proxy.conf.json`, `vite.config.ts` — point at the
  gateway only.

**M1 — auth hardening**
- `services/auth-service/src/AuthService.Infrastructure/.../JwtTokenService.cs` — add
  `tenant_id` + `perms` claims.
- `services/auth-service/src/AuthService.Api/*` — seed-admin path.
- `apps/angular-client/.../core/auth/*`, `apps/react-admin/.../modules/auth/*` — refresh
  interceptor; customer app — OTP screens.

## K. What must NOT be changed

- The 4-project Clean-Architecture layout, MediatR/FluentValidation/EF Core choice, native
  OpenAPI + Scalar (not Swashbuckle), per-service database/schema, transactional outbox
  pattern — these are correct; reuse them.
- `.ai/*` rule files — they are the contract, not drift.
- Working migrations in auth/bus/route/payment/notification — do not regenerate; add new
  migrations only.
- auth-service's refresh-rotation + theft-detection, lockout, audit trail, RBAC entities —
  extend, don't replace.
- Existing passing tests — do not weaken to make a build green.
- `.git` history — never rewrite, force-push or reinitialise.
- Framework versions (.NET 10, Angular 22, React 19) — do not up/downgrade except the
  notification-service EF Core 9→10 alignment (P1-18).
- The empty `docs/*` stub files — leave until the relevant milestone fills them.

## L. Production risks (summary)

1. **Financial:** payment confirm is client-trusted (P0-5); webhook signature bypass (P0-6);
   refunds don't reach the PSP (P0-7); Nagad protocol invented (P1-8); bKash webhook check
   fabricated (P1-7). Any of these = money loss or free tickets.
2. **Data / privacy:** booking IDOR (P0-9); customer id from request body (P0-10);
   cross-tenant payment read (P0-11); unauthenticated notification history exposes PII (P0-8).
3. **Availability:** no gateway (P0-1); in-memory rate limiting + Quartz + outbox not
   multi-instance safe (P1-13, P1-14, P1-15) → scale-out causes duplicates and starvation;
   auto-migrate on startup crashes a service if the DB is briefly unreachable.
4. **Functional:** booking has no schema (P0-3); event routing keys don't match (P0-4);
   no payment→booking confirmation (P1-1); held seats leak (P1-2); seat double-booking
   (P1-3); no ticket generation (P1-6); templates unseeded (P1-10).
5. **Operability:** unobservable (P0-13); shared/empty-string JWT keys (P0-12); dev
   credentials in compose; no CI/CD.

## M. Recommended testing strategy

- **Per milestone:** the affected services' existing xUnit unit + integration suites must
  stay green; add targeted tests for each fixed finding (referenced in E/F above).
- **Concurrency:** the booking NBomber test (`services/booking-service/performance-tests/
  nbomber/`) is the gate for P1-3 (0 double-books) and P1-2 (holds expire).
- **Idempotency / messaging:** new integration tests — publish each domain event, assert
  exactly one downstream effect; replay the same delivery, assert still one.
- **Security:** authz tests per protected endpoint (owner/non-owner/admin); tenant-isolation
  tests (no-header cross-tenant read → 404); forged-webhook test; login/OTP brute-force →
  rate-limited.
- **Payment sandbox:** bKash + Nagad sandbox happy-path + tamper-rejection + reconciliation,
  run in CI against the sandbox (never against production PSPs).
- **Resilience:** provider-500 → backoff; provider-timeout-on-execute → pending-verification
  (not retried); broker-down → outbox row retained.
- **Frontend:** critical-flow component/service tests (auth incl. refresh, search, booking,
  payment error handling, guards) in both apps.
- **Load:** k6/JMeter against the gateway for the search + booking + payment path before
  each production release; document thresholds.
- **CI (M11):** restore → build → unit → integration (Testcontainers) → frontend tests →
  docker build → contract tests, per service.
