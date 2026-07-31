# API examples — real requests and responses

Every endpoint the Booking Service exposes today, with real payloads you can
copy-paste. Same data as the Postman collection (`postman/`) and the seed
script (`scripts/seed-demo-data.sql`) — run that first so these `tripId`
values actually resolve.

All authenticated endpoints need `Authorization: Bearer <token>` — see
`postman/README.md` for how the collection mints one automatically, or use
the same trick manually.

---

## `GET /api/v1/trips/search` — public, no auth

**Request**
```
GET /api/v1/trips/search?origin=Dhaka&destination=Chattogram&date=2026-08-15&page=1&pageSize=10
```

**Response — 200 OK**
```json
{
  "items": [
    {
      "tripId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "originCity": "Dhaka",
      "destinationCity": "Chattogram",
      "departureUtc": "2026-08-15T02:00:00Z",
      "arrivalUtc": "2026-08-15T08:00:00Z",
      "busType": "AC Sleeper",
      "operatorPlateNumber": "DHK-1234",
      "pricePerSeat": 1500.00,
      "currency": "BDT",
      "availableSeats": 36,
      "totalSeats": 36
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 10,
  "totalPages": 1
}
```
Response headers also include `X-Pagination` and `X-Total-Count` — see
[API_PAGINATION.md](./API_PAGINATION.md).

---

## `POST /api/v1/bookings` — requires auth

**Request**
```json
POST /api/v1/bookings
Authorization: Bearer <token>
Content-Type: application/json

{
  "tripId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "customerId": "00000000-0000-0000-0000-000000000001",
  "passengers": [
    { "seatNumber": "A1", "fullName": "Porosh Ahmed", "age": 30, "gender": "Male" }
  ]
}
```

**Response — 201 Created**
```json
{
  "bookingId": "b2c3d4e5-1234-4a5b-8c9d-0e1f2a3b4c5d",
  "tripId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "customerId": "00000000-0000-0000-0000-000000000001",
  "status": "PendingPayment",
  "totalAmount": 1500.00,
  "currency": "BDT",
  "createdAtUtc": "2026-08-01T09:00:00Z",
  "holdExpiresAtUtc": "2026-08-01T09:10:00Z",
  "seats": [
    { "seatNumber": "A1", "passengerFullName": "Porosh Ahmed" }
  ]
}
```

**Response — 409 Conflict** (seat taken between your search and this call)
```json
{
  "type": "https://httpstatuses.io/409",
  "title": "Seat 'A1' on trip '3fa85f64-5717-4562-b3fc-2c963f66afa6' is no longer available.",
  "status": 409,
  "traceId": "00-...-01"
}
```

---

## `GET /api/v1/bookings/{bookingId}` — requires auth

**Request**
```
GET /api/v1/bookings/b2c3d4e5-1234-4a5b-8c9d-0e1f2a3b4c5d
Authorization: Bearer <token>
```

**Response — 200 OK**: same shape as the `POST /bookings` response above.

**Response — 404 Not Found**
```json
{
  "type": "https://httpstatuses.io/404",
  "title": "Booking 'b2c3d4e5-1234-4a5b-8c9d-0e1f2a3b4c5d' was not found.",
  "status": 404,
  "traceId": "00-...-01"
}
```

---

## `POST /api/v1/bookings/{bookingId}/cancel` — requires auth

**Request**
```json
POST /api/v1/bookings/b2c3d4e5-1234-4a5b-8c9d-0e1f2a3b4c5d/cancel
Authorization: Bearer <token>
Content-Type: application/json

{
  "customerId": "00000000-0000-0000-0000-000000000001",
  "reason": "Change of travel plans"
}
```

**Response — 204 No Content** (empty body)

---

## `GET /health` — no auth

**Response — 200 OK** (or 503 if a dependency is down)
```json
{
  "status": "Healthy",
  "results": {
    "postgres": { "status": "Healthy" },
    "rabbitmq": { "status": "Healthy" },
    "redis": { "status": "Healthy" }
  }
}
```

---

## Try these live

- Scalar: http://localhost:8080/scalar — click any endpoint, "Try it"
- Postman: import `postman/Bus-Ticketing-Booking-Service.postman_collection.json` — bearer token attached automatically
