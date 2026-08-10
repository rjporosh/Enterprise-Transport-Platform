# Testing — Integration Tests

## Framework
- Microsoft.AspNetCore.Mvc.Testing (`WebApplicationFactory<Program>`)
- Testcontainers (Postgres, RabbitMQ, Redis)
- FluentAssertions

## Location
`tests/RouteService.IntegrationTests/`

## Prerequisites
- Docker daemon running
- Sufficient memory (3 containers + test host)

## Running
```bash
dotnet test tests/RouteService.IntegrationTests
```

## What is tested
- Health endpoint returns 200
- Unauthenticated requests return 401
- Release info endpoint is publicly accessible
- Full lifecycle (create route, stop, schedule) can be exercised

## Notes
- Integration tests mint their own JWT locally (no Auth Service dependency)
- Each test run uses isolated containers
- Tests are not run in CI by default due to Docker requirement
