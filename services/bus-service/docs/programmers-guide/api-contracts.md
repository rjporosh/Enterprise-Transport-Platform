# Programmer's Guide — API Contracts

## Base URL

```
http://localhost:5201
```

## Authentication

All endpoints except `/health`, `/metrics`, `/scalar`, and `/api/v1/release-info` require a Bearer JWT issued by **Auth Service**.

```
Authorization: Bearer <token>
```

## Endpoints

### Buses

| Method | Route | Auth | Roles | Description |
|---|---|---|---|---|
| `POST` | `/api/v1/buses` | Bearer | Operator, Admin | Register a new bus |
| `GET` | `/api/v1/buses/{busId}` | Bearer | Any authenticated | Get bus by ID |
| `GET` | `/api/v1/buses` | Bearer | Any authenticated | Search buses (filterable) |
| `PUT` | `/api/v1/buses/{busId}` | Bearer | Operator, Admin | Update bus details |
| `POST` | `/api/v1/buses/{busId}/status` | Bearer | Operator, Admin | Change bus status |
| `DELETE` | `/api/v1/buses/{busId}` | Bearer | Operator, Admin | Soft delete bus |
| `POST` | `/api/v1/buses/{busId}/restore` | Bearer | Operator, Admin | Restore soft-deleted bus |

### Depots

| Method | Route | Auth | Roles | Description |
|---|---|---|---|---|
| `POST` | `/api/v1/depots` | Bearer | Admin | Create depot |
| `GET` | `/api/v1/depots` | Bearer | Any authenticated | List depots (optionally by city) |
| `DELETE` | `/api/v1/depots/{depotId}` | Bearer | Admin | Soft delete depot |
| `POST` | `/api/v1/depots/{depotId}/restore` | Bearer | Admin | Restore depot |

### Utility

| Method | Route | Auth | Description |
|---|---|---|---|
| `GET` | `/health` | None | Health check (Postgres, Redis, RabbitMQ) |
| `GET` | `/metrics` | None | Prometheus metrics |
| `GET` | `/scalar` | None (Dev only) | Interactive API docs |
| `GET` | `/api/v1/release-info` | None | Service version + feature flags |

## Request/Response Examples

### Register Bus

```http
POST /api/v1/buses HTTP/1.1
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
Content-Type: application/json

{
  "operatorId": "a1b2c3d4-...",
  "plateNumber": "DHA-1234",
  "busType": "AcSleeper",
  "totalSeats": 40,
  "depotId": "e5f6g7h8-...",
  "manufacturer": "Volvo",
  "model": "9600",
  "yearOfManufacture": 2022,
  "tenantId": "i9j0k1l2-...",
  "companyId": "m3n4o5p6-...",
  "organizationId": "q7r8s9t0-..."
}
```

```json
{
  "success": true,
  "message": "Bus registered successfully.",
  "data": {
    "id": "b1c2d3e4-...",
    "operatorId": "a1b2c3d4-...",
    "plateNumber": "DHA-1234",
    "busType": "AcSleeper",
    "totalSeats": 40,
    "depotId": "e5f6g7h8-...",
    "status": "Active",
    "manufacturer": "Volvo",
    "model": "9600",
    "yearOfManufacture": 2022,
    "isDeleted": false,
    "createdAtUtc": "2026-08-10T12:00:00Z",
    "updatedAtUtc": "2026-08-10T12:00:00Z"
  },
  "traceId": "00-abc123-..."
}
```

### Get Bus

```http
GET /api/v1/buses/{busId} HTTP/1.1
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

```json
{
  "success": true,
  "data": {
    "id": "b1c2d3e4-...",
    "operatorId": "a1b2c3d4-...",
    "plateNumber": "DHA-1234",
    "busType": "AcSleeper",
    "totalSeats": 40,
    "depotId": "e5f6g7h8-...",
    "status": "Active",
    "manufacturer": "Volvo",
    "model": "9600",
    "yearOfManufacture": 2022,
    "isDeleted": false,
    "createdAtUtc": "2026-08-10T12:00:00Z",
    "updatedAtUtc": "2026-08-10T12:00:00Z"
  },
  "traceId": "00-abc123-..."
}
```

### List Buses

```http
GET /api/v1/buses?status=Active&page=1&pageSize=20 HTTP/1.1
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

```json
{
  "success": true,
  "data": {
    "items": [ ... ],
    "totalCount": 42,
    "page": 1,
    "pageSize": 20
  },
  "traceId": "00-abc123-..."
}
```

### Change Bus Status

```http
POST /api/v1/buses/{busId}/status HTTP/1.1
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
Content-Type: application/json

{
  "newStatus": "UnderMaintenance"
}
```

## Error Responses

All errors follow the structure:

```json
{
  "success": false,
  "message": "One or more validation errors occurred.",
  "errors": [
    {
      "code": "VALIDATION_ERROR",
      "field": "plateNumber",
      "message": "Plate number is required."
    }
  ],
  "traceId": "00-abc123-..."
}
```

| Status | Title | Example Code |
|---|---|---|
| 400 | Validation error / Invalid status transition | `VALIDATION_ERROR`, `INVALID_STATUS_TRANSITION` |
| 401 | Unauthorized | — |
| 403 | Forbidden (insufficient role) | — |
| 404 | Not found | `BUS_NOT_FOUND`, `DEPOT_NOT_FOUND` |
| 409 | Conflict | `DUPLICATE_PLATE_NUMBER`, `CONCURRENCY_CONFLICT` |
| 429 | Rate limit exceeded | — |
| 500 | Unexpected error | `INTERNAL_ERROR` |
