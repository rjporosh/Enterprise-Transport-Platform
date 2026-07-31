# Enterprise Transport Platform (Bus Ticketing)

A reference platform demonstrating modern software architecture — the
business domain is bus ticketing, but the point is the engineering: Clean
Architecture, CQRS, event-driven services, full observability, and a real
customer + admin frontend, all wired together and runnable, not just
diagrammed.

`MASTER_SPEC.md` and `ROADMAP.md` describe the full target system (8
services, multi-tenant SaaS, mobile clients). This repo currently implements
**one complete vertical slice** of that plan, built for real rather than
scaffolded. See "What's built" below for exactly what that covers — and what
doesn't, yet.

## What's built

| Layer | Location | Status |
|---|---|---|
| Booking Service (backend) | `services/booking-service` | **.NET 10.** Search trips, create/cancel booking, seat-hold concurrency (Postgres `xmin`), transactional outbox → RabbitMQ, Redis cache-aside, unit + integration tests, k6 load/stress tests |
| Observability | `infrastructure/monitoring/`, wired into `docker-compose.yml` | OpenTelemetry traces → Jaeger, metrics → Prometheus → Grafana, structured logs → Seq |
| API documentation | `services/booking-service/src/BookingService.Api` | Swagger UI (`/swagger`) and Scalar (`/scalar`) with real filled-in examples, not empty schemas; Postman collection with auto-attached bearer tokens in `postman/` |
| Customer web app | `apps/angular-client/bus-ticketing-customer-web` | **Angular 19** (target is 22 — not yet upgraded, see "Known gaps" below). Search → seat selection → booking confirmation flow, standalone components + signals |
| Admin console | `apps/react-admin/bus-ticketing-admin` | React 19, TanStack Query. Bookings list + detail, cancel-booking action |
| Local orchestration | `infrastructure/docker/docker-compose.yml` | Postgres, RabbitMQ, Redis, Seq, Jaeger, Prometheus, Grafana, and all three apps wired together |

Everything else under `services/`, `apps/`, `shared/`, and `infrastructure/`
that isn't listed above is still the scaffold from the original plan — empty
folders reserving a place in the architecture, not yet built.

## Known gaps (next up, in order)

1. **Angular 19 → 22.** The customer app still targets Angular 19;
   upgrading the package versions, build config, and any breaking-change
   fallout hasn't been done yet.
2. **React 19 → whatever's current.** Similarly not revisited yet.
3. **NBomber / JMeter** — k6 covers both load and stress testing for this
   slice (see `services/booking-service/tests/load/`); the other tools
   named in `MASTER_SPEC.md` aren't set up.
4. Everything else in `ROADMAP.md` past this slice (Auth Service, Payment
   Service, multi-tenancy, RBAC beyond a bearer check, mobile clients).

## A note on how this was built

The first version of this repo was generated in a sandboxed environment with
**no .NET SDK, no npm registry access, and no network** — nothing was
compiled. Since then, the backend has been rebuilt against **.NET 10** and
extended with Redis, OpenTelemetry, Seq, Prometheus, Grafana, Scalar/OpenAPI,
and k6 load/stress tests — still hand-written and manually reviewed (brace
balance, import consistency, cross-project references), **not yet
CI-verified**, in this same constrained environment. One spot flagged
explicitly for a second look: the OpenTelemetry Redis instrumentation call in
`Program.cs` uses a parameterless API surface that's version-sensitive — see
the comment there.

Before you rely on this, or attach it to something like a job application,
**run it**:

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

# 3. Whole stack, including the observability tools
cd ../../infrastructure/docker
docker compose up --build
```

| Service | URL |
|---|---|
| Customer web | http://localhost:4200 |
| Admin console | http://localhost:5173 |
| API — Swagger | http://localhost:8080/swagger |
| API — Scalar | http://localhost:8080/scalar |
| Seq (logs) | http://localhost:8081 |
| Jaeger (traces) | http://localhost:16686 |
| Prometheus | http://localhost:9090 |
| Grafana | http://localhost:3000 (admin/admin) |

Fix whatever the compiler/`ng build`/`vite build` surface — treat this as a
strong, current draft of a real codebase, not a finished one.

## Repository layout

```
services/booking-service/   Clean Architecture .NET 10 service (see its own README)
apps/angular-client/        Customer-facing booking flow (Angular)
apps/react-admin/           Operations console (React)
infrastructure/docker/      docker-compose for local dev, including observability stack
infrastructure/monitoring/  Prometheus scrape config + Grafana provisioning/dashboards
postman/                    Postman collection with auto-bearer-token pre-request script
scripts/seed-demo-data.sql  Sample routes/buses/trips for local testing
docs/, MASTER_SPEC.md, ROADMAP.md   Full target-state plan (mostly still unimplemented)
```
