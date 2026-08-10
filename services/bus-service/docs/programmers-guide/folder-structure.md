# Programmer's Guide — Folder Structure

```
services/bus-service/
├── src/
│   ├── BusService.Api/
│   │   ├── Diagnostics/
│   │   │   └── RuntimeErrorLogWriter.cs
│   │   ├── Endpoints/
│   │   │   └── BusEndpoints.cs
│   │   ├── Grpc/
│   │   │   └── BusGrpcService.cs
│   │   ├── Middleware/
│   │   │   ├── CorrelationIdMiddleware.cs
│   │   │   ├── ExceptionHandlingMiddleware.cs
│   │   │   ├── IdempotencyMiddleware.cs
│   │   │   ├── IpTracingMiddleware.cs
│   │   │   └── RequestContextMiddleware.cs
│   │   ├── Security/
│   │   │   ├── ClientInfoExtensions.cs
│   │   │   └── CurrentUser.cs
│   │   ├── Program.cs
│   │   ├── Properties/
│   │   │   └── launchSettings.json
│   │   ├── Protos/
│   │   │   └── bus.proto
│   │   └── appsettings*.json
│   ├── BusService.Application/
│   │   ├── Common/
│   │   │   ├── Behaviors/
│   │   │   │   ├── LoggingBehavior.cs
│   │   │   │   └── ValidationBehavior.cs
│   │   │   ├── Interfaces/
│   │   │   │   ├── IAuditLogger.cs
│   │   │   │   ├── IBusDbContext.cs
│   │   │   │   ├── IBusMetrics.cs
│   │   │   │   ├── ICacheService.cs
│   │   │   │   ├── ICurrentUser.cs
│   │   │   │   ├── IDateTimeProvider.cs
│   │   │   │   ├── IEventPublisher.cs
│   │   │   │   └── ILocalizationService.cs
│   │   │   └── Models/
│   │   │       ├── BusDto.cs
│   │   │       ├── DepotDto.cs
│   │   │       ├── PagedResult.cs
│   │   │       └── Result.cs
│   │   ├── DependencyInjection.cs
│   │   └── Features/
│   │       ├── Buses/
│   │       │   ├── RegisterBus/
│   │       │   ├── GetBus/
│   │       │   ├── GetBuses/
│   │       │   ├── UpdateBusDetails/
│   │       │   ├── ChangeBusStatus/
│   │       │   ├── SoftDeleteBus/
│   │       │   └── RestoreBus/
│   │       └── Depots/
│   │           ├── CreateDepot/
│   │           ├── GetDepots/
│   │           ├── SoftDeleteDepot/
│   │           └── RestoreDepot/
│   ├── BusService.Domain/
│   │   ├── Common/
│   │   │   ├── AggregateRoot.cs
│   │   │   ├── DomainEvent.cs
│   │   │   └── Entity.cs
│   │   ├── Entities/
│   │   │   ├── Bus.cs
│   │   │   └── Depot.cs
│   │   ├── Enums/
│   │   │   ├── BusStatus.cs
│   │   │   └── BusType.cs
│   │   ├── Events/
│   │   │   ├── BusRegisteredDomainEvent.cs
│   │   │   ├── BusDetailsUpdatedDomainEvent.cs
│   │   │   ├── BusStatusChangedDomainEvent.cs
│   │   │   ├── BusSoftDeletedDomainEvent.cs
│   │   │   └── BusRestoredDomainEvent.cs
│   │   └── Exceptions/
│   │       ├── BusNotFoundException.cs
│   │       ├── ConcurrencyException.cs
│   │       ├── DepotNotFoundException.cs
│   │       ├── DomainException.cs
│   │       ├── DuplicatePlateNumberException.cs
│   │       └── InvalidBusStatusTransitionException.cs
│   └── BusService.Infrastructure/
│       ├── Auditing/
│       │   └── AuditLogger.cs
│       ├── Caching/
│       │   ├── RedisCacheService.cs
│       │   └── RedisOptions.cs
│       ├── Common/
│       │   └── DateTimeProvider.cs
│       ├── DependencyInjection.cs
│       ├── Localization/
│       │   └── JsonLocalizationService.cs
│       ├── Messaging/
│       │   ├── IMessageBusPublisher.cs
│       │   ├── RabbitMqOptions.cs
│       │   └── RabbitMqPublisher.cs
│       ├── Migrations/
│       ├── Observability/
│       │   ├── BusMetrics.cs
│       │   └── FileLogging/
│       ├── Persistence/
│       │   ├── BusDbContext.cs
│       │   ├── Configurations/
│       │   │   ├── BusConfiguration.cs
│       │   │   ├── DepotConfiguration.cs
│       │   │   ├── AuditLogConfiguration.cs
│       │   │   └── OutboxMessageConfiguration.cs
│       │   └── Outbox/
│       │       ├── OutboxEventPublisher.cs
│       │       ├── OutboxMessage.cs
│       │       └── OutboxProcessor.cs
│       └── BusDbContextDesignTimeFactory.cs
├── tests/
│   ├── BusService.UnitTests/
│   │   ├── Buses/
│   │   │   ├── BusTests.cs
│   │   │   ├── ChangeBusStatusHandlerTests.cs
│   │   │   └── RegisterBusHandlerTests.cs
│   │   ├── TestSupport/
│   │   │   ├── FakeBusMetrics.cs
│   │   │   ├── FakeCacheService.cs
│   │   │   ├── FakeCurrentUser.cs
│   │   │   ├── FakeDateTimeProvider.cs
│   │   │   ├── FakeEventPublisher.cs
│   │   │   └── TestBusDbContext.cs
│   │   └── BusService.UnitTests.csproj
│   ├── BusService.IntegrationTests/
│   │   ├── BusApiTests.cs
│   │   └── BusService.IntegrationTests.csproj
│   └── load/
│       └── k6/
│           └── bus-service-load-test.js
├── docs/
│   ├── db-schema.md
│   ├── diagrams/
│   │   └── c4/
│   │       ├── context.md
│   │       ├── container.md
│   │       ├── component.md
│   │       ├── deployment.md
│   │       └── code.md
│   ├── programmers-guide/
│   │   ├── getting-started.md
│   │   ├── api-contracts.md
│   │   ├── cqrs.md
│   │   ├── validation.md
│   │   ├── grpc.md
│   │   ├── consuming-events.md
│   │   ├── repository.md
│   │   ├── migrations.md
│   │   ├── testing.md
│   │   ├── background-workers.md
│   │   └── adding-entity.md
│   ├── scripts/
│   │   └── postman/
│   │       └── bus-service.postman-collection.json
│   └── testing/
│       ├── unit.md
│       ├── integration.md
│       ├── functional.md
│       └── performance.md
├── BusService.sln
└── README.md
```

## Naming Conventions

- **Files**: PascalCase for classes, `*.cs` extension.
- **Folders**: PascalCase for features (`Buses`, `Depots`), PascalCase for infrastructure sub-folders (`Caching`, `Messaging`).
- **Endpoints**: `Map<Feature>Async` private methods in `BusEndpoints.cs`.
- **DTOs**: `*Dto.cs`, `*Request.cs`, `*Response.cs`.
- **Tests**: `*Tests.cs`, co-located in `tests/<Project>/<Feature>/`.
