# Testing — Functional Tests

## Scope

Functional tests verify the service from an external consumer's perspective — HTTP, gRPC, and event flows. These run in CI against the full Docker Compose stack.

## Scenarios

| ID | Scenario | Expected Result |
|---|---|---|
| FT-001 | `POST /api/v1/depots` with Admin role | `200 OK` + depot created |
| FT-002 | `POST /api/v1/depots` with Operator role | `403 Forbidden` |
| FT-003 | `POST /api/v1/buses` with duplicate plate | `409 Conflict` |
| FT-004 | `POST /api/v1/buses/{id}/status` to Retired then back to Active | `400 Bad Request` |
| FT-005 | `GET /api/v1/buses` with city filter | Returns only buses in that city |
| FT-006 | `DELETE /api/v1/buses/{id}` then `GET` | `404 Not Found` (soft-deleted) |
| FT-007 | `POST /api/v1/buses/{id}/restore` then `GET` | `200 OK` + bus restored |
| FT-008 | gRPC `GetBus` for non-existent ID | `NOT_FOUND` status |
| FT-009 | Event `bus.registered` published to RabbitMQ | Consumer receives message |
| FT-010 | Health check when Postgres is down | `503 Service Unavailable` |

## Running

Functional tests are typically run in CI (GitHub Actions) after `docker compose up -d`. Locally:

```bash
docker compose -f infrastructure/docker/docker-compose.yml up -d
cd services/bus-service
dotnet test tests/functional
```

## Traceability

Each functional test maps to a user story or acceptance criterion in `docs/srs/Acceptance_Criteria.md`.
