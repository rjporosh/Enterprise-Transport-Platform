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
| Shared UI library | `apps/shared-ui-library` | Implemented: shared design tokens + Tailwind preset, parallel Angular/React component sets (Button, Card, Badge, Input, Spinner, Modal, DataTable, PageHeader, StatCard, EmptyState, Pagination, Toast), consumed as source via path aliases by both apps |
| Customer web app | `apps/angular-client/bus-ticketing-customer-web` | **Angular 22.** Implemented: search → seat selection → booking confirmation → mock payment → e-ticket flow; auth (login/register); protected "My Bookings" page with cancel; site header/nav; 404 page. A `mockApiInterceptor` (`environment.mockApi`) answers every request in-browser, so the full purchase journey is demoable with no backend running |
| Admin console | `apps/react-admin/bus-ticketing-admin` | **React 19.** Implemented: auth (login + protected routes), Dashboard (KPIs + recent bookings), Bookings (list/detail/cancel), Trips, Buses, Routes, Users — all backed by a custom mock axios adapter (`VITE_USE_MOCK_API`) for the same no-backend-required demo |
| Local orchestration | `infrastructure/docker/docker-compose.yml` | Implemented: Postgres, RabbitMQ, and all three apps wired together |

Everything else under `services/`, `apps/`, `shared/`, and `infrastructure/`
that isn't listed above is still the scaffold from the original plan — empty
folders reserving a place in the architecture, not yet built. Both frontend
apps ship a `CONTRIBUTING-NEW-CRUD.md` with a concrete, file-by-file guide
(with a worked example) for filling one of those in. Building the backend
services next, one real slice at a time, is what `ROADMAP.md` sequences —
today only booking-service is real; every other API the frontends call
(auth, trips, payments, fleet/routes, admin users) is simulated by the mock
layers described above so the UI can be built and demoed ahead of the
backend.

## A note on how this was built

This code was generated in a sandboxed environment with **no .NET SDK, no
Node/npm, and no network** — so nothing here has actually been compiled or
run by a build tool. Every file was hand-written with production patterns in
mind and manually cross-checked (import paths, data contracts between the
frontend services and the mock API layers, tsconfig/vite alias resolution),
but that's a meaningfully lower bar than a green CI pipeline. Before you rely
on this, or attach it to something like a job interview demo, **run it**:

```bash
# 1. Backend (optional for the frontend demo — both apps run against mock
#    data out of the box; wire this up when you're ready to go end-to-end)
cd services/booking-service
dotnet restore
dotnet build
dotnet test tests/BookingService.UnitTests

# Generate the initial EF Core migration (none are checked in)
dotnet ef migrations add InitialCreate \
  --project src/BookingService.Infrastructure \
  --startup-project src/BookingService.Api

# 2. Customer web app (Angular 22) — runs standalone via mockApiInterceptor
cd apps/angular-client/bus-ticketing-customer-web
npm install
npm start
# http://localhost:4200

# 3. Admin console (React 19) — runs standalone via the mock axios adapter
cd apps/react-admin/bus-ticketing-admin
npm install
npm run dev
# http://localhost:5173  (sign in with any email/password)

# 4. Whole stack, once the backend is wired up
cd infrastructure/docker
docker compose up --build
# API + Swagger: http://localhost:8080/swagger
```

Fix whatever the compiler/`ng build`/`vite build` surfaces — treat this as a
strong first draft of a real codebase, not a finished one. To go from demo
to production, flip `environment.mockApi` (Angular) and `VITE_USE_MOCK_API`
(React) to `false` once the real backend services exist at their configured
base URLs — no other frontend code needs to change, since every service
already calls the real REST shape.

## Repository layout

```
services/booking-service/   Clean Architecture .NET service (see its own README)
apps/shared-ui-library/     Shared design tokens + parallel Angular/React component libraries
apps/angular-client/        Customer-facing booking flow (Angular 22)
apps/react-admin/           Operations console (React 19)
infrastructure/docker/      docker-compose for local dev
docs/, MASTER_SPEC.md, ROADMAP.md   Full target-state plan (mostly still unimplemented)
```
