# Documentation index

Start here. This folder is the full target-state plan for the platform
(`MASTER_SPEC.md` / `ROADMAP.md` at the repo root) — most of it is still a
scaffold (empty files reserving a place in the structure). The links below
are **populated with real content** and are the ones worth reading first;
everything else under `docs/` follows the same folder layout for when those
pieces get built.

## Start here (populated)

| Doc | What it's for |
|---|---|
| [RUNBOOK.md](./RUNBOOK.md) | Step-by-step: clone → build → migrate → seed → run → hit an endpoint. Start here if you're new. |
| [OBSERVABILITY_GUIDE.md](./OBSERVABILITY_GUIDE.md) | Step-by-step: use Seq/Grafana/Jaeger/Prometheus, with exact queries — find a slow request, see its trace, see its logs, see its metrics. |
| [CRUD_GUIDE_BACKEND.md](./CRUD_GUIDE_BACKEND.md) | Add a new CRUD feature to a .NET service, end to end, following this repo's vertical-slice pattern. |
| [CRUD_GUIDE_ANGULAR.md](./CRUD_GUIDE_ANGULAR.md) | Add a new feature (list/detail/create) to the Angular customer app. |
| [CRUD_GUIDE_REACT.md](./CRUD_GUIDE_REACT.md) | Add a new module (list/detail/mutate) to the React admin console. |
| [api/API_PAGINATION.md](./api/API_PAGINATION.md) | The pagination contract every list endpoint follows (defaults, response header shape). |
| [api/API_EXAMPLES.md](./api/API_EXAMPLES.md) | Real request/response JSON for every Booking Service endpoint. |
| [database/Database_Design.md](./database/Database_Design.md) | Actual schema (tables, columns, indexes) as implemented, not aspirational. |
| [diagrams/C4_Context.md](./diagrams/C4_Context.md) | C4 Level 1 — the system in its environment. |
| [diagrams/C4_Container.md](./diagrams/C4_Container.md) | C4 Level 2 — services, datastores, how they talk. |
| [diagrams/C4_Component.md](./diagrams/C4_Component.md) | C4 Level 3 — inside the Booking Service (Clean Architecture layers). |
| [diagrams/C4_Code.md](./diagrams/C4_Code.md) | C4 Level 4 — the Booking/Trip aggregate class shapes. |
| [diagrams/ERD.md](./diagrams/ERD.md) | Entity-relationship diagram matching the real EF Core configuration. |
| [diagrams/Sequence_Diagrams.md](./diagrams/Sequence_Diagrams.md) | The create-booking flow end to end, including the outbox and cache eviction. |
| [architecture/Architecture_Overview.md](./architecture/Architecture_Overview.md) | Narrative version of the C4 diagrams above. |

## Everything else (scaffold, not yet written)

`00_Project_Charter.md`, `01_Project_Vision.md`, `02_Goals_and_Scope.md`,
`03_Glossary.md`, `adr/*` (beyond what's referenced above), `api/API_Contracts.md`,
`api/API_Guidelines.md`, `api/Error_Responses.md`, `api/Versioning.md`,
`architecture/*` (CQRS, DDD, Event_Driven_Architecture, High_Availability,
Microservice_Boundaries, Scalability, Vertical_Slice, Clean_Architecture),
`database/Concurrency.md`, `database/Indexing.md`, `database/Partitioning.md`,
`database/Tables.md`, `development/*`, `devops/*`, `diagrams/Activity_Diagrams.md`,
`diagrams/Deployment_Diagram.md`, `testing/*` — these are placeholders from
the original project scaffold. Pick one and write it the same way as the
populated docs above: real content matching what's actually built, not a
generic template.

## Per-service / per-app docs

- [`services/booking-service/README.md`](../services/booking-service/README.md) — architecture, how to run, what's implemented
- [`services/booking-service/performance-tests/`](../services/booking-service/performance-tests/) — k6, JMeter, NBomber, each with their own README
- [`postman/README.md`](../postman/README.md) — the Postman collection and its auto-bearer-token trick
