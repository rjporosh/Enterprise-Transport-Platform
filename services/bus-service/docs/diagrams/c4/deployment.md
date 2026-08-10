# C4 Deployment Diagram

```mermaid
graph TD
    subgraph "Developer Machine / CI"
        Docker["Docker Compose"]
        DotNet[".NET SDK"]
    end

    subgraph "Container: bus-ticketing"
        subgraph "bus-service Container"
            BusApp["BusService.Api<br/>(dotnet BusService.Api.dll)"]
        end
        subgraph "booking-service Container"
            BookingApp["BookingService.Api"]
        end
        subgraph "postgres-bus Container"
            PostgresBus["PostgreSQL 16<br/>(bus_service)"]
        end
        subgraph "postgres Container"
            PostgresBooking["PostgreSQL 16<br/>(booking_service)"]
        end
        subgraph "rabbitmq Container"
            Rabbit["RabbitMQ 3.13<br/>(amqp://guest:guest@rabbitmq:5672)"]
        end
        subgraph "redis Container"
            RedisNode["Redis 7.4<br/>(redis:6379)"]
        end
    end

    subgraph "External"
        Client["Browser / Mobile App"]
        Monitoring["Prometheus / Grafana / OTLP"]
    end

    Client -->|"HTTP/HTTPS :5201"| BusApp
    Client -->|"HTTP/HTTPS :8080"| BookingApp
    BusApp -->|"TCP :5432"| PostgresBus
    BookingApp -->|"TCP :5432"| PostgresBooking
    BusApp -->|"AMQP :5672"| Rabbit
    BookingApp -->|"AMQP :5672"| Rabbit
    BusApp -->|"TCP :6379"| RedisNode
    BusApp -->|"OTLP/gRPC :4317"| Monitoring
    BookingApp -->|"OTLP/gRPC :4317"| Monitoring
```

## Description

- **Bus Service** runs in its own container, built from `src/BusService.Api/Dockerfile`.
- **PostgreSQL** is isolated per-service (`bus_service` database on port 5434, `booking_service` on 5432) to prevent coupling.
- **RabbitMQ** is shared — services communicate via exchanges (`bus.events`, `booking.events`, etc.).
- **Redis** is shared for caching across services.
- **Health checks** verify Postgres, Redis, and RabbitMQ availability before the app starts accepting traffic.
- **OpenTelemetry** exports traces and metrics to an OTLP collector (e.g., Jaeger, Prometheus).
