# C4 Context Diagram

```mermaid
graph TD
    User["User / Passenger"]
    Admin["Admin / Operator"]
    Frontend["Frontend (React)"]
    Gateway["API Gateway (YARP/Ocelot)"]
    AuthService["Auth Service"]
    BookingService["Booking Service"]
    PaymentService["Payment Service"]
    NotificationService["Notification Service"]
    RabbitMQ["RabbitMQ Broker"]
    Postgres[(PostgreSQL)]
    SMTP["SMTP Provider"]
    SMS["SMS Provider"]
    FCM["Firebase Cloud Messaging"]

    User --> Frontend
    Admin --> Frontend
    Frontend --> Gateway
    Gateway --> NotificationService
    AuthService --> RabbitMQ
    BookingService --> RabbitMQ
    PaymentService --> RabbitMQ
    RabbitMQ --> NotificationService
    NotificationService --> Postgres
    NotificationService --> SMTP
    NotificationService --> SMS
    NotificationService --> FCM
    NotificationService --> RabbitMQ
    AuthService -.-> NotificationService
    BookingService -.-> NotificationService
    PaymentService -.-> NotificationService
```

## Description

- **Users** interact with the platform through the Frontend, which routes through the API Gateway.
- **Auth/Booking/Payment Services** publish domain events to RabbitMQ.
- **Notification Service** consumes upstream events and sends notifications via email, SMS, and push.
- **Notification Service** exposes REST and gRPC endpoints for direct invocation.
- Data is persisted in **PostgreSQL**.
