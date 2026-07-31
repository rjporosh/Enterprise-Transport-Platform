# Enterprise Transport Platform (Bus Ticketing)

A reference platform demonstrating modern software architecture — the
business domain is bus ticketing, but the point is the engineering: Clean
Architecture, CQRS, event-driven services, full observability, and a real
customer + admin frontend, all wired together and runnable, not just
diagrammed.

📚 **Start with [`docs/README.md`](./docs/README.md)** for the full
documentation index, or [`docs/RUNBOOK.md`](./docs/RUNBOOK.md) to go
straight to "clone this and get a booking working."

`MASTER_SPEC.md` and `ROADMAP.md` describe the full target system (8
services, multi-tenant SaaS, mobile clients). This repo currently implements
**one complete vertical slice** of that plan, built for real rather than
scaffolded.

## What's built

| Layer | Location | Status |
|---|---|---|
| Booking Service (backend) | `services/booking-service` | **.NET 10.** Search trips (paginated, header metadata), create/cancel booking, seat-hold concurrency (Postgres `xmin`), transactional outbox → RabbitMQ, Redis cache-aside |
| Observability | `infrastructure/monitoring/`, wired into `docker-compose.yml` | OpenTelemetry traces → Jaeger, metrics → Prometheus → Grafana, structured logs → Seq — see [`docs/OBSERVABILITY_GUIDE.md`](./docs/OBSERVABILITY_GUIDE.md) for exact queries |
| Performance testing | `services/booking-service/performance-tests/` | k6 (primary), JMeter, and NBomber — same two scenarios (search load, seat-contention stress) in all three |
| API documentation | `services/booking-service/src/BookingService.Api` | Native OpenAPI + **Scalar** at `/scalar` (Swagger dropped — see booking-service README for why); real examples in [`docs/api/API_EXAMPLES.md`](./docs/api/API_EXAMPLES.md); Postman collection with auto-attached bearer tokens in `postman/` |
| Architecture docs | `docs/` | C4 diagrams (Context/Container/Component/Code), ERD, sequence diagrams — all in `docs/diagrams/`, all Mermaid (renders natively on GitHub) |
| Customer web app | `apps/angular-client/bus-ticketing-customer-web` | **Angular 19** (target is 22 — not yet upgraded, see "Known gaps"). Search → seat selection → booking confirmation flow |
| Admin console | `apps/react-admin/bus-ticketing-admin` | React 19, TanStack Query. Bookings list + detail, cancel-booking action |
| Local orchestration | `infrastructure/docker/docker-compose.yml` | Postgres, RabbitMQ, Redis, Seq, Jaeger, Prometheus, Grafana, and all three apps wired together |

Everything else under `services/`, `apps/`, `shared/`, and `infrastructure/`
that isn't listed above is still the scaffold from the original plan — empty
folders reserving a place in the architecture, not yet built.

## Known gaps (next up, in order)

1. **Auth Service** — not built. All auth today is "trust a JWT signed with
   a shared dev key" (see `postman/README.md`'s pre-request script for how
   that dev token gets minted). No real user registration/login flow exists
   yet.
2. **Angular 19 → 22** and **React version bump** — not yet done.
3. Everything else in `ROADMAP.md` past this slice (Payment Service,
   Notification Service, API Gateway, multi-tenancy, RBAC beyond a bearer
   check, mobile clients).

## A note on how this was built

The first version of this repo was generated in a sandboxed environment with
**no .NET SDK, no npm registry access, and no network** — nothing was
compiled. Since then the backend has been rebuilt against **.NET 10**,
extended with Redis/OpenTelemetry/Seq/Prometheus/Grafana/Scalar and
k6+JMeter+NBomber performance tests, and had a real "builds but crashes on
run" bug fixed (mixed-up OpenTelemetry/health-check registration, and a
Swashbuckle-vs-native-OpenAPI version conflict — see the booking-service
README's "Why Scalar only" section). Still hand-written and manually
reviewed in that same constrained environment, **not CI-verified**. Before
you rely on this, or attach it to something like a job application, **run
it** — see [`docs/RUNBOOK.md`](./docs/RUNBOOK.md).

```bash
cd services/booking-service
dotnet restore && dotnet build && dotnet test tests/BookingService.UnitTests

cd ../../infrastructure/docker
docker compose up --build
```

| Service | URL |
|---|---|
| Customer web | http://localhost:4200 |
| Admin console | http://localhost:5173 |
| API — Scalar | http://localhost:8080/scalar |
| Seq (logs) | http://localhost:8081 |
| Jaeger (traces) | http://localhost:16686 |
| Prometheus | http://localhost:9090 |
| Grafana | http://localhost:3000 (admin/admin) |

## Repository layout

```
services/booking-service/   Clean Architecture .NET 10 service (see its own README)
apps/angular-client/        Customer-facing booking flow (Angular)
apps/react-admin/           Operations console (React)
infrastructure/docker/      docker-compose for local dev, including observability stack
infrastructure/monitoring/  Prometheus scrape config + Grafana provisioning/dashboards
postman/                    Postman collection with auto-bearer-token pre-request script
scripts/seed-demo-data.sql  Sample routes/buses/trips for local testing
docs/                       Full documentation — start at docs/README.md
```
