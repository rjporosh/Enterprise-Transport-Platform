# Troubleshooting

## Build fails with `CS0234` (namespace/type name conflict)
Ensure all `using` directives in feature folders fully-qualify domain entities
when the feature folder name matches the entity name (e.g. `Domain.Entities.Route`
instead of bare `Route`).

## EF Core crashes at runtime: "entity type 'DomainEvent' requires a primary key"
Ensure `AggregateRoot.DomainEvents` and `AggregateRoot.Version` are explicitly
`Ignore()`d in every `IEntityTypeConfiguration<T>` where `T : AggregateRoot`.

## Health checks report unhealthy but service appears fine
- Check that `ConnectionStrings:RouteDb` is correct and the DB is reachable
- Verify Redis and RabbitMQ are running and credentials match
- Ensure `Database:Provider` matches the actual DB engine

## `DbUpdateConcurrencyException` on every update
The `Version` column must be mapped as `IsConcurrencyToken()` in the entity
configuration and the handler must pass the expected version in the command.

## Outbox messages are not published
- Verify RabbitMQ is reachable and the `route.events` exchange exists
- Check `outbox_messages` table for `Error` and `RetryCount` columns
- Ensure `OutboxProcessor` hosted service is running

## Localization returns key instead of translation
- Verify the `.resx` files are set to `Embedded Resource`
- Ensure the resource path matches the namespace `RouteService.Infrastructure.Localization.Resources.Messages`

## Rate limiting is not applied
Ensure `app.UseRateLimiter()` is called in `Program.cs` before endpoint mapping,
and that endpoints use `.RequireRateLimiting("write")` where needed.

## gRPC service returns `Unimplemented`
Ensure `app.MapGrpcService<RouteGrpcServiceImpl>()` is registered and the proto
package/namespace matches the generated code.

## Tests fail with `Docker` not found
Integration tests require a local Docker daemon. Start Docker Desktop or the
Docker engine before running `dotnet test` on the integration test project.

## Logs directory missing
The `logs/` directory is created on demand. Ensure the service process has write
permissions to the content root.
