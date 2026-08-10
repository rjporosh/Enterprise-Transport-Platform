# C4 Container Diagram

```mermaid
graph TD
    subgraph "Bus Service"
        Api["BusService.Api<br/>(ASP.NET Core Minimal API)"]
        Application["BusService.Application<br/>(CQRS / MediatR)"]
        Infrastructure["BusService.Infrastructure<br/>(EF Core, RabbitMQ, Redis)"]
        Domain["BusService.Domain<br/>(Entities, Events, Exceptions)"]
    end

    subgraph "External Dependencies"
        Postgres[(PostgreSQL)]
        RabbitMQ[RabbitMQ]
        RedisCache[(Redis)]
        Auth["Auth Service<br/>(JWT Validation)"]
    end

    Api --> Application
    Application --> Domain
    Application --> Infrastructure
    Infrastructure --> Domain
    Infrastructure --> Postgres
    Infrastructure --> RabbitMQ
    Infrastructure --> RedisCache
    Api --> Auth

    classDef container fill:#08427b,stroke:#333,stroke-width:2px,color:#fff
    classDef external fill:#666,stroke:#333,stroke-width:1px,color:#fff
    class Api,Application,Infrastructure,Domain container
    class Postgres,RabbitMQ,RedisCache,Auth external
```

## Description

- **BusService.Api**: Minimal API endpoints, JWT auth, OpenAPI/Scalar, gRPC, health checks, middleware pipeline.
- **BusService.Application**: CQRS commands/queries, validators, domain event handlers, DTOs.
- **BusService.Infrastructure**: EF Core repositories, outbox publisher/processor, Redis cache, query logging interceptor.
- **BusService.Domain**: Aggregate roots (`Bus`, `Depot`), domain events, exceptions, enums, value objects.
- **PostgreSQL**: Primary datastore with provider portability (Postgres/SqlServer/MySql).
- **RabbitMQ**: Event publishing via transactional outbox pattern.
- **Redis**: Response caching for read endpoints.
- **Auth Service**: External JWT validation — same signing key/issuer/audience configuration.
