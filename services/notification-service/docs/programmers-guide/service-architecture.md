# Service Architecture

## Clean Architecture Layers

```
Api (controllers, endpoints, middleware)
    ↓
Application (commands, queries, validators, port interfaces)
    ↓
Domain (entities, value objects, domain events, exceptions)
    ↓
Infrastructure (EF Core, RabbitMQ, SMTP, SMS, Push, Quartz, templates)
```

## Key Design Decisions

1. **API never calls channel providers directly** — sends are deferred to `NotificationDispatchJob` (Quartz). Keeps API latency bounded by DB insert time.
2. **Outbox pattern** — events are staged in the same DB transaction as the aggregate change, then relayed to RabbitMQ by `OutboxProcessor`.
3. **State machine** — `Notification` enforces all transitions. Illegal transitions throw `InvalidNotificationStateException`.
4. **Provider abstraction** — `IEmailSender`, `ISmsSender`, `IPushSender` allow runtime provider switching via configuration.
5. **Database provider abstraction** — `Database:Provider` selects EF Core provider at startup.

## Dependency Rules

- Domain has zero framework dependencies (only `MediatR.Contracts` for `INotification`)
- Application depends on Domain + MediatR + FluentValidation + EF Core abstractions
- Infrastructure implements Application ports
- Api composes everything
