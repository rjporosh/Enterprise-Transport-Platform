# Postman collection

Real requests against the Booking Service — import both files and go.

## Import

1. Postman -> Import -> select both `Bus-Ticketing-Booking-Service.postman_collection.json` and `Local.postman_environment.json`.
2. Select the **Bus Ticketing — Local** environment (top-right dropdown).
3. Run `docker compose up` (see `infrastructure/docker/`) and seed data with `scripts/seed-demo-data.sql`.

## How the bearer token gets attached automatically

Every request in this collection inherits a **collection-level pre-request
script** (see the collection's "Pre-request Script" tab, or `Scripts.md`
below) that:

1. Mints a fresh HS256 JWT in-browser using Postman's built-in `CryptoJS`,
   signed with the same dev-only key baked into `appsettings.json` /
   `docker-compose.yml`.
2. Sets it as `{{access_token}}` in the environment.
3. Attaches it as `Authorization: Bearer <token>` to the request that's
   about to run — via `pm.request.headers.upsert(...)`.

You never have to run a separate "login" request first, and you never have
to manually copy a token into a header — every single request in the
collection gets one, including new ones you add to this collection later
(the script lives at the collection level, not per-request).

This only works locally because the signing key is a shared dev secret. It
is not, and should never be, how a real auth flow works — see
`services/booking-service/README.md` for what a real Auth Service issuing
real tokens would look like.

## Suggested run order

1. **Health / Liveness check** — confirms the stack is up.
2. **Trips / Search trips** — also captures `{{tripId}}` from the response into the environment.
3. **Bookings / Create booking** — uses `{{tripId}}`, captures `{{bookingId}}`.
4. **Bookings / Get booking by id** — uses `{{bookingId}}`.
5. **Bookings / Cancel booking** — uses `{{bookingId}}`; releases the seat.

Or run the whole folder with Postman's Collection Runner / Newman:

```bash
npm install -g newman
newman run Bus-Ticketing-Booking-Service.postman_collection.json \
  -e Local.postman_environment.json
```
