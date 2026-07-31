# Runbook: from clone to a working booking

Step-by-step, in order. Every step assumes you're in the repo root unless
stated otherwise.

## Prerequisites

- .NET 10 SDK
- Node.js 22+
- Docker + Docker Compose
- `psql` client (for seeding data) — or use any Postgres GUI

## 1. Start infrastructure

```bash
cd infrastructure/docker
docker compose up -d postgres rabbitmq redis seq jaeger prometheus grafana
```

Wait ~15s for healthchecks to pass:

```bash
docker compose ps
# postgres, rabbitmq, redis should all show "healthy"
```

## 2. Generate and apply EF Core migrations

**No migrations are checked into this repo yet.** This is the #1 cause of
"it builds, but nothing works" — without a migration, the database has no
tables, and every request fails with "relation does not exist".

```bash
cd services/booking-service
dotnet tool install --global dotnet-ef   # if you don't have it
dotnet restore

dotnet ef migrations add InitialCreate \
  --project src/BookingService.Infrastructure \
  --startup-project src/BookingService.Api

dotnet ef database update \
  --project src/BookingService.Infrastructure \
  --startup-project src/BookingService.Api
```

Confirm tables exist:

```bash
psql "postgresql://booking_svc:changeme@localhost:5432/booking_service" -c "\dt booking.*"
```

You should see `routes`, `buses`, `trips`, `trip_seats`, `bookings`,
`booking_seats`, `outbox_messages`.

## 3. Seed demo data

```bash
psql "postgresql://booking_svc:changeme@localhost:5432/booking_service" \
  -f ../../scripts/seed-demo-data.sql
```

This creates one normal trip (`3fa85f64-5717-4562-b3fc-2c963f66afa6`, 36
seats, Dhaka -> Chattogram) and one deliberately tiny trip
(`44444444-4444-4444-4444-444444444444`, 4 seats — for the stress test).

## 4. Run the Booking Service

```bash
dotnet build
dotnet test tests/BookingService.UnitTests    # should be green
dotnet run --project src/BookingService.Api
```

Check it's alive:

```bash
curl http://localhost:8080/health
```

## 5. Try an actual request

```bash
curl "http://localhost:8080/api/v1/trips/search?origin=Dhaka&destination=Chattogram&date=2026-08-15" -i
```

You should get a `200`, a JSON body with `items`, and an `X-Pagination`
response header — see [api/API_PAGINATION.md](./api/API_PAGINATION.md).

For the authenticated endpoints (`POST /bookings`, etc.) either:
- Import `postman/` into Postman — it mints a dev JWT automatically (see
  `postman/README.md`), or
- Use `/scalar` (below) and paste a token manually, or
- Get a token via the same trick documented in `postman/README.md`'s
  pre-request script.

## 6. Browse the API docs

- Scalar (interactive, click-to-try): http://localhost:8080/scalar
- Raw OpenAPI document: http://localhost:8080/openapi/v1.json
- Real example payloads: [api/API_EXAMPLES.md](./api/API_EXAMPLES.md)

## 7. Run the frontends

```bash
# Customer web
cd apps/angular-client/bus-ticketing-customer-web
npm install
npm start          # http://localhost:4200

# Admin console (new terminal)
cd apps/react-admin/bus-ticketing-admin
npm install
npm run dev         # http://localhost:5173
```

## 8. Check observability is actually wired up

Follow [OBSERVABILITY_GUIDE.md](./OBSERVABILITY_GUIDE.md) — it walks through
making a request, then finding its trace in Jaeger, its logs in Seq, and its
metrics in Grafana, with the exact queries to paste.

## 9. Run a load/stress test

See `services/booking-service/performance-tests/` — pick k6, JMeter, or
NBomber (each has its own README with exact commands).

## Or: run everything via docker compose

```bash
cd infrastructure/docker
docker compose up --build
```

This builds and runs all three apps plus the whole infra/observability
stack. You still need to run steps 2-3 (migrations + seed) against the
containerized Postgres the first time.

## Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| "relation \"booking.trips\" does not exist" | No migrations applied | Step 2 |
| 401 on `POST /bookings` | No/expired bearer token | Get one via Postman or the RUNBOOK step 5 links |
| `docker compose up` fails on `redis`/`seq`/etc. port already in use | Something else on your machine is using that port | Change the host-side port mapping in `infrastructure/docker/docker-compose.yml` |
| Booking Service can't reach Redis/RabbitMQ/Postgres | Compose service name vs `localhost` mismatch — running `dotnet run` outside Docker but Redis/etc. also in Docker should still work since ports are published to `localhost`; if you changed `appsettings.json` connection strings to use container names (`postgres`, `redis`, ...) instead of `localhost`, that only works from *inside* another container | Use `localhost` connection strings when running the API outside Docker |
