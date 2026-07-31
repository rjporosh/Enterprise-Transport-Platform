# Enterprise Transport Platform (Bus Ticketing)

A reference platform demonstrating modern software architecture — the
business domain is bus ticketing, but the point is the engineering: Clean
Architecture, CQRS, event-driven services, and a real customer + admin
frontend, all wired together and runnable, not just diagrammed.

`MASTER_SPEC.md` and `ROADMAP.md` describe the full target system (8
services, multi-tenant SaaS, full observability stack, mobile clients). This
repo currently implements **one complete vertical slice** of that plan —
built for real, end-to-end, rather than scaffolding every folder with empty
stubs. See "What's built" below for exactly what that covers.

## What's built

| Layer | Location | Status |
|---|---|---|
| Booking Service (backend) | `services/booking-service` | Implemented: search trips, create/cancel booking, seat-hold concurrency, transactional outbox → RabbitMQ, unit + integration tests |
| Customer web app | `apps/angular-client/bus-ticketing-customer-web` | Implemented: search → seat selection → booking confirmation flow (Angular 22, standalone components, signals) |
| Admin console | `apps/react-admin/bus-ticketing-admin` | Implemented: bookings list + detail, cancel-booking action (React 19, TanStack Query) |
| Local orchestration | `infrastructure/docker/docker-compose.yml` | Implemented: Postgres, RabbitMQ, and all three apps wired together |

Everything else under `services/`, `apps/`, `shared/`, and `infrastructure/`
that isn't listed above is still the scaffold from the original plan — empty
folders reserving a place in the architecture, not yet built. Building
those next, one real slice at a time, is what `ROADMAP.md` sequences.

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
# API + Swagger: http://localhost:8080/swagger
```

Fix whatever the compiler/`ng build`/`vite build` surface — treat this as a
strong first draft of a real codebase, not a finished one.

## Repository layout

```
services/booking-service/   Clean Architecture .NET service (see its own README)
apps/angular-client/        Customer-facing booking flow (Angular)
apps/react-admin/           Operations console (React)
infrastructure/docker/      docker-compose for local dev
docs/, MASTER_SPEC.md, ROADMAP.md   Full target-state plan (mostly still unimplemented)
```
