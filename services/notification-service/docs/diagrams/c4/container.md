# C4 Container Diagram

```mermaid
graph TD
    subgraph "Notification Service"
        Api["Api Layer\nREST + gRPC + Middleware"]
        App["Application Layer\nCQRS, Validators, Ports"]
        Domain["Domain Layer\nEntities, Events, Rules"]
        Infra["Infrastructure Layer\nEF Core, RabbitMQ, SMTP, SMS, Push, Quartz"]
    end
    Postgres[(PostgreSQL)]
    RabbitMQ["RabbitMQ"]
    SMTP["SMTP Server"]
    SMS["SMS Provider"]
    FCM["Firebase"]

    Api --> App
    App --> Domain
    App --> Infra
    Infra --> Domain
    Infra --> Postgres
    Infra --> RabbitMQ
    Infra --> SMTP
    Infra --> SMS
    Infra --> FCM
```

## Container Descriptions

| Container | Technology | Responsibility |
|---|---|---|
| Api | ASP.NET Core Web API | REST + gRPC endpoints, middleware, auth, rate limiting, OpenTelemetry |
| Application | .NET Class Library | MediatR handlers, FluentValidation, Result Pattern, port interfaces |
| Domain | .NET Class Library | Aggregate roots, domain events, business rules, zero framework deps |
| Infrastructure | .NET Class Library | EF Core, RabbitMQ, MailKit SMTP, Twilio/GenericHttp SMS, FCM push, Quartz, Scriban |
