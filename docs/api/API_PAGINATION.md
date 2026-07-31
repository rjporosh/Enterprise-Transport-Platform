# Pagination contract

Every list endpoint in this platform follows the same contract. Today
that's just `GET /api/v1/trips/search`; use this same shape for any new
paginated endpoint (see `docs/CRUD_GUIDE_BACKEND.md`).

## Request

Query string parameters, both optional:

| Param | Type | Default if omitted or `<= 0` |
|---|---|---|
| `page` | int | `1` |
| `pageSize` | int | `10` |

```
GET /api/v1/trips/search?origin=Dhaka&destination=Chattogram&date=2026-08-15
GET /api/v1/trips/search?origin=Dhaka&destination=Chattogram&date=2026-08-15&page=2&pageSize=25
```

The first call above returns page 1, 10 results — you don't have to pass
`page`/`pageSize` to get sane defaults.

## Response body

```json
{
  "items": [ /* array of results */ ],
  "totalCount": 47,
  "page": 1,
  "pageSize": 10,
  "totalPages": 5
}
```

## Response headers

Pagination metadata is **also** returned as a response header, so a client
that only needs the count/page info (e.g. for building "Next" button state)
doesn't need to parse the body:

```
X-Pagination: {"currentPage":1,"pageSize":10,"totalCount":47,"totalPages":5,"hasPrevious":false,"hasNext":true}
X-Total-Count: 47
```

React admin's `httpClient` (axios) exposes this as
`response.headers['x-pagination']` (parse the JSON string yourself) —
see `docs/CRUD_GUIDE_REACT.md`.

## Implementation reference

`Api/Endpoints/TripsEndpoints.cs` — the `page`/`pageSize` parameters are
bound as nullable `int?`; `null` or `<= 0` triggers the default. The header
is set via `httpContext.Response.Headers.Append(...)` before returning
`Results.Ok(result)`. Copy this exact pattern for any new list endpoint.
