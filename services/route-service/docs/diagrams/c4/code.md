# Diagrams — C4 Code

See `docs/diagrams/c4/component.md` for component-level view.

## Key Classes

| Namespace | Responsibility |
|-----------|---------------|
| `RouteService.Domain.Entities.Route` | Aggregate root for routes |
| `RouteService.Domain.Entities.Stop` | Aggregate root for stops |
| `RouteService.Domain.Entities.Schedule` | Aggregate root for schedules |
| `RouteService.Application.Features.Routes.CreateRoute` | CreateRouteCommand + Handler |
| `RouteService.Infrastructure.Persistence.RouteDbContext` | EF Core DbContext |
| `RouteService.Api.Endpoints.RouteEndpoints` | REST route endpoints |
| `RouteService.Api.Grpc.RouteGrpcServiceImpl` | gRPC implementation |
| `RouteService.Infrastructure.Persistence.Outbox.OutboxProcessor` | Background outbox publisher |
