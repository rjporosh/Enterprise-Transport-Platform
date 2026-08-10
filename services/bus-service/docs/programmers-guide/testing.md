# Programmer's Guide — Testing

## Unit Tests

Unit tests live in `tests/BusService.UnitTests/` and cover:

- Domain entity behavior (`BusTests.cs`)
- Handler logic (`RegisterBusHandlerTests.cs`, `ChangeBusStatusHandlerTests.cs`)
- Fakes for all interfaces (`FakeBusMetrics`, `FakeCacheService`, `FakeCurrentUser`, `FakeDateTimeProvider`, `FakeEventPublisher`)

Run:

```bash
cd services/bus-service
dotnet test tests/BusService.UnitTests
```

## Integration Tests

Integration tests live in `tests/BusService.IntegrationTests/` and use:

- `WebApplicationFactory<Program>` to spin up the full API in-memory.
- Testcontainers for **PostgreSQL**, **RabbitMQ**, and **Redis** — real dependencies, not mocks.

Run:

```bash
cd services/bus-service
dotnet test tests/BusService.IntegrationTests
```

> **Note**: Requires a running Docker daemon.

## Functional Tests

Functional tests live in `tests/functional/` and exercise the API via HTTP (or gRPC) using the full deployed stack. These are run in CI against the Docker Compose environment.

## Performance Tests

Load tests live in `tests/load/k6/` and use [k6](https://k6.io/).

```bash
k6 run tests/load/k6/bus-service-load-test.js
```

See `tests/load/k6/` for scenarios.
