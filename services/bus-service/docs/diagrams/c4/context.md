# C4 Context Diagram

```mermaid
graph TD
    User["User / Passenger"]
    Admin["Admin / Operator"]
    Frontend["Frontend (React / Angular)"]
    Gateway["API Gateway (YARP/Ocelot)"]
    AuthService["Auth Service"]
    BookingService["Booking Service"]
    NotificationService["Notification Service"]
    BusService["Bus Service"]
    RabbitMQ["RabbitMQ Broker"]
    Postgres[(PostgreSQL)]
    Redis[(Redis)]
    GrpcClient["gRPC Client"]

    User --> Frontend
    Admin --> Frontend
    Frontend --> Gateway
    Gateway --> BusService
    AuthService -.-> BusService
    BookingService -.-> BusService
    BusService --> RabbitMQ
    BusService --> Postgres
    BusService --> Redis
    GrpcClient --> BusService
```

## Description

- **Users** interact with the platform through the Frontend, which routes through the API Gateway.
- **Auth Service** issues JWTs that Bus Service validates for authentication and authorization.
- **Booking Service** consumes bus domain events published by Bus Service to keep its read model in sync.
- **Notification Service** may consume bus events for operational alerts (e.g., bus retired).
- **Bus Service** publishes domain events to RabbitMQ (`bus.events` exchange).
- Data is persisted in **PostgreSQL** with **Redis** for caching.
- **gRPC** is exposed for internal service-to-service communication.
