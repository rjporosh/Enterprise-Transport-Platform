# Bus Service — Test Cases

## Functional Test Cases

| ID | Scenario | Preconditions | Steps | Expected Result |
|---|---|---|---|---|
| FT-001 | Create depot as Admin | Valid JWT with Admin role | `POST /api/v1/depots` with valid body | `200 OK`, depot created |
| FT-002 | Create depot as Operator | Valid JWT with Operator role | `POST /api/v1/depots` with valid body | `403 Forbidden` |
| FT-003 | Create depot without auth | None | `POST /api/v1/depots` | `401 Unauthorized` |
| FT-004 | Register bus with duplicate plate | Existing bus with same plate | `POST /api/v1/buses` | `409 Conflict` |
| FT-005 | Register bus with invalid depot | DepotId does not exist | `POST /api/v1/buses` | `404 Not Found` or validation error |
| FT-006 | Get bus by ID | Bus exists | `GET /api/v1/buses/{id}` | `200 OK`, bus data returned |
| FT-007 | Get bus by ID (not found) | Bus does not exist | `GET /api/v1/buses/{id}` | `404 Not Found` |
| FT-008 | List buses (no filter) | Buses exist | `GET /api/v1/buses` | `200 OK`, paged list |
| FT-009 | List buses (filter by status) | Buses with status Active exist | `GET /api/v1/buses?status=Active` | `200 OK`, only Active buses |
| FT-010 | List buses (filter by depot) | Buses in depot exist | `GET /api/v1/buses?depotId={id}` | `200 OK`, filtered list |
| FT-011 | Update bus details | Bus exists, user is Admin/Operator | `PUT /api/v1/buses/{id}` | `200 OK`, updated fields |
| FT-012 | Update bus plate (forbidden) | Bus exists | `PUT /api/v1/buses/{id}` with new plate | `200 OK` (plate unchanged) |
| FT-013 | Change status Active → UnderMaintenance | Bus is Active | `POST /api/v1/buses/{id}/status` | `200 OK`, status = UnderMaintenance |
| FT-014 | Change status UnderMaintenance → Active | Bus is UnderMaintenance | `POST /api/v1/buses/{id}/status` | `200 OK`, status = Active |
| FT-015 | Change status Active → Retired | Bus is Active | `POST /api/v1/buses/{id}/status` | `200 OK`, status = Retired |
| FT-016 | Change status Retired → Active | Bus is Retired | `POST /api/v1/buses/{id}/status` | `400 Bad Request` |
| FT-017 | Soft delete bus | Bus exists | `DELETE /api/v1/buses/{id}` | `200 OK`, bus marked deleted |
| FT-018 | Get soft-deleted bus | Bus is soft-deleted | `GET /api/v1/buses/{id}` | `404 Not Found` |
| FT-019 | Restore soft-deleted bus | Bus is soft-deleted | `POST /api/v1/buses/{id}/restore` | `200 OK`, bus restored |
| FT-020 | List depots (no filter) | Depots exist | `GET /api/v1/depots` | `200 OK`, list of depots |
| FT-021 | List depots (filter by city) | Depots in city exist | `GET /api/v1/depots?city=Dhaka` | `200 OK`, filtered list |
| FT-022 | Health check | All dependencies up | `GET /health` | `200 OK`, all checks pass |
| FT-023 | Health check (Postgres down) | Postgres down | `GET /health` | `503 Service Unavailable` |
| FT-024 | gRPC GetBus | Bus exists | `GetBus(busId)` | `GetBusResponse` with data |
| FT-025 | gRPC GetBus (not found) | Bus does not exist | `GetBus(busId)` | `NOT_FOUND` status |

## Unit Test Cases

| ID | Scenario | Expected Result |
|---|---|---|
| UT-001 | Bus.Register creates bus with Active status | Status == Active, events raised |
| UT-002 | Bus.ChangeStatus Active → UnderMaintenance | Status == UnderMaintenance, event raised |
| UT-003 | Bus.ChangeStatus Active → Retired | Status == Retired, event raised |
| UT-004 | Bus.ChangeStatus Retired → Active | Throws `InvalidBusStatusTransitionException` |
| UT-005 | Bus.SoftDelete sets IsDeleted and Status = Retired | IsDeleted == true, Status == Retired |
| UT-006 | Bus.Restore clears IsDeleted and Status = Active | IsDeleted == false, Status == Active |
| UT-007 | Depot.Create sets name and city | Name == input, City == input |
| UT-008 | Depot.SoftDelete sets IsDeleted | IsDeleted == true |
| UT-009 | RegisterBusValidator rejects empty plate | Validation fails with message |
| UT-010 | RegisterBusValidator rejects plate > 20 chars | Validation fails with message |
| UT-011 | ChangeBusStatusValidator rejects invalid status | Validation fails with message |
| UT-012 | RegisterBusHandler checks duplicate plate | Throws `DuplicatePlateNumberException` |
| UT-013 | GetBusHandler returns NotFound for missing bus | Throws `BusNotFoundException` |
| UT-014 | GetBusesHandler returns paginated result | Result.TotalCount > 0, Page == 1 |

## Performance Test Cases

| ID | Scenario | Target |
|---|---|---|
| PT-001 | Read-heavy (50 VUs, GET /buses) | p95 < 200ms, error rate < 1% |
| PT-002 | Write burst (5 req/s, POST /buses) | p95 < 500ms, error rate < 1% |
| PT-003 | Cache hit (single bus, repeated GET) | p95 < 100ms, hit ratio > 95% |
| PT-004 | Rate limit exceeded (write policy) | `429 Too Many Requests` after 20 requests/minute |
| PT-005 | Concurrent status changes | No deadlocks, all succeed or fail gracefully |
