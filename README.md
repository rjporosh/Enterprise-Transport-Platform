# Enterprise Transport Platform (Bus Ticketing)

A reference platform demonstrating modern software architecture — the
business domain is bus ticketing, but the point is the engineering: Clean
Architecture, CQRS, event-driven services, and a real customer + admin
frontend, all wired together and runnable, not just diagrammed.

`MASTER_SPEC.md` and `ROADMAP.md` describe the full target system (8+
services, multi-tenant SaaS, full observability stack, mobile clients). This
repo currently has **six backend services scaffolded** plus a customer and an
admin frontend. Bus ticketing is **not yet production-ready** — see
[`docs/PRODUCTION-GAP-ANALYSIS.md`](docs/PRODUCTION-GAP-ANALYSIS.md) for the
audited state (≈ 35–40% for bus ticketing, ≈ 15–20% overall SaaS),
[`docs/PRODUCTION-MILESTONES.md`](docs/PRODUCTION-MILESTONES.md) for the path
to production, and [`docs/API-GAPS.md`](docs/API-GAPS.md) for the
endpoint-level gap register. "What's built" below is a high-level summary;
those three documents are authoritative.

## What's built

| Layer | Location | Status |
| |---|---|
| Auth Service (backend) | `services/auth-service` | Implemented: register/login/refresh (rotation + theft detection)/logout, account lockout, audit trail, DB-provider-switchable EF Core, transactional outbox → RabbitMQ, unit + integration tests |
| Booking Service (backend) | `services/booking-service` | Partial: search trips, create/cancel booking, seat-hold, transactional outbox → RabbitMQ, unit + integration tests. **Gaps: 0 EF migrations checked in, no payment-event consumer, no expired-hold release, endpoints are IDOR / take customer id from the body, no "My Bookings" — see docs/PRODUCTION-GAP-ANALYSIS.md** |
| Bus Service (backend) | `services/bus-service` | Implemented: fleet/depot management, bus lifecycle (Active/UnderMaintenance/Retired), DB-provider-switchable EF Core, transactional outbox → RabbitMQ, file-based build/runtime/query diagnostic logging (see `scripts/README.md`), unit + integration tests |
| Route Service (backend) | `services/route-service` | Implemented: routes, stops, schedules with full CRUD, soft delete, optimistic concurrency, DB-provider-switchable EF Core, transactional outbox → RabbitMQ, unit + integration tests, comprehensive docs |
| Payment Service (backend) | `services/payment-service` | Partial: payment state machine, provider abstraction (Default/bKash/Nagad/Stripe), create/process/confirm/fail/cancel/refund + webhook endpoints, Quartz reconciliation jobs, 5 migrations. **Unsafe: confirm is client-trusted, webhook signature is bypassable, refunds never reach the PSP, tenant isolation uses a spoofable header. bKash webhook check is fabricated; Nagad protocol is invented; no Bangla QR — see docs/PRODUCTION-GAP-ANALYSIS.md** |
| Notification Service (backend) | `services/notification-service` | Partial: email (SMTP), SMS (Twilio/generic), push (FCM), Scriban templates, RabbitMQ consumer, Quartz dispatch, 3 migrations. **Gaps: `POST /notifications` + history unauthenticated, no template seeding, in-memory idempotency + Quartz (not multi-instance safe), pinned to EF Core 9, no Bangladesh SMS provider, no PDF attachments — see docs/PRODUCTION-GAP-ANALYSIS.md** |
| Customer web app | `apps/angular-client/bus-ticketing-customer-web` | Partial: search → seat selection → booking confirmation (Angular 22, standalone, signals). **Payment page is a simulated card form; My Bookings is served from an in-app mock; no token refresh, no i18n, no tests** |
| Admin console | `apps/react-admin/bus-ticketing-admin` | Partial: auth, buses list, routes+stops, booking detail + cancel (React 19, TanStack Query). **Dashboard stats / users / bookings-list / trips-list served from in-app mocks; no token refresh, no i18n, no tests** |
| Local orchestration | `infrastructure/docker/docker-compose.yml` | Postgres ×6, RabbitMQ, Redis, MailHog, the 6 APIs, both frontends. **No API gateway (`infrastructure/gateway/` is empty). No observability backend.** |

Not yet built: **API gateway** (YARP), **Ticketing service** (ticket number /
QR / PDF / verification), shared kernel (`shared/*` is empty), observability
stack, CI/CD, multi-tenant subscription foundation. See
[`docs/PRODUCTION-MILESTONES.md`](docs/PRODUCTION-MILESTONES.md).

## A note on how this was built

This code was generated in a sandboxed environment with **no .NET SDK, no
npm registry access, and no network** — so nothing here has actually been
compiled or run by a build tool. Every file was hand-written with production
patterns in mind (see the Booking Service README for the specific
architectural decisions), and manually reviewed for brace balance, import
consistency, and cross-project reference correctness. That's a meaningfully
lower bar than a green CI pipeline. Before you rely on this, or attach it to
something like a job application, **run it**:

```bash
# 1. Backend
cd services/booking-service
dotnet restore
dotnet build
dotnet test tests/BookingService.UnitTests

# 2. Generate the initial EF Core migration (none are checked in)
dotnet ef migrations add InitialCreate \
  --project src/BookingService.Infrastructure \
  --startup-project src/BookingService.Api

# 3. Whole stack
cd ../../infrastructure/docker
docker compose up --build
# customer web:  http://localhost:4200
# admin console: http://localhost:5173
# API docs (Scalar, per service): http://localhost:8080/scalar  (booking-service)
#   — this platform uses native OpenAPI + Scalar, not Swagger/Swashbuckle
```

Fix whatever the compiler/`ng build`/`vite build` surface — treat this as a
strong first draft of a real codebase, not a finished one.

## Repository layout

```
services/                   6 Clean Architecture .NET 10 services:
                            auth, booking, bus, route, payment, notification
apps/angular-client/        Customer-facing booking flow (Angular 22)
apps/react-admin/           Operations console (React 19)
shared/                     Intended shared kernel — currently EMPTY (see docs/PRODUCTION-GAP-ANALYSIS.md P0-2)
infrastructure/docker/      docker-compose for local dev
infrastructure/gateway/     Intended API gateway — currently EMPTY (P0-1)
docs/PRODUCTION-GAP-ANALYSIS.md, docs/PRODUCTION-MILESTONES.md, docs/API-GAPS.md
                            Authoritative audited state + path to production (2026-08-31)
docs/, MASTER_SPEC.md, ROADMAP.md   Full target-state plan (mostly still unimplemented)
```
