# Route Service — AI Handover

## Status
- **Built**: Yes (not compiled — run `dotnet build` to verify)
- **Tests**: Unit test skeletons + integration test skeleton with Testcontainers
- **Docs**: architecture.md, db-schema.md, roadmap.md, this file, release notes

## What is built
- Full Clean Architecture + CQRS (MediatR) + FluentValidation
- Domain: Route, Stop, RouteStop, Schedule aggregates with state machines
- Application: Result pattern, localization (en/bn), pipeline behaviors
- Infrastructure: EF Core (provider-switchable), RabbitMQ outbox, Redis cache, Polly resilience, audit logging
- API: REST (Scalar), gRPC, Serilog, OpenTelemetry, health checks, rate limiting
- Tests: xUnit unit tests (InMemory), integration test skeleton (Testcontainers)
- Performance: k6, NBomber, JMeter templates

## Known gaps
- No real migration generated yet (provider-specific; run `dotnet ef migrations add InitialCreate`)
- No Booking Service sync consumer for route events
- Rate limiting is fixed-window only; configurable per-endpoint is future work
- Resilience policies are hardcoded; should be configurable via `Resilience:` section
- Audit log IP/correlation ID population is manual; currently only user ID is populated
- gRPC protobufs are not compiled into generated code yet (requires `dotnet grpc` tooling)

## How to run
1. `cd services/route-service`
2. `dotnet build`
3. `dotnet run --project src/RouteService.Api`
4. Open `http://localhost:5003/scalar` in Development

## Environment variables / secrets
- `ConnectionStrings:RouteDb`
- `Jwt:SigningKey`
- `Redis:ConnectionString`
- `RabbitMq:*`
- `Database:Provider`

## Next AI steps
1. Compile-verify and fix any build errors
2. Generate real EF Core migration for target provider
3. Implement Booking Service sync consumer (if Booking Service is ready)
4. Wire release-info endpoint into CI/CD pipeline for SQA
5. Expand unit tests to cover all state transitions and concurrency conflicts
