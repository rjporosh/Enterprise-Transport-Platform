# C4 Deployment Diagram

```mermaid
graph TD
    subgraph "Docker / Kubernetes"
        subgraph "Notification Service Pod"
            AppContainer["NotificationService.Api Container\n(ASP.NET Core, port 8080)"]
            Sidecar["OpenTelemetry Collector Sidecar"]
        end
        PostgresContainer["PostgreSQL Container\n(port 5432)"]
        RabbitMQContainer["RabbitMQ Container\n(port 5672, management 15672)"]
        PrometheusContainer["Prometheus Container\n(port 9090)"]
        GrafanaContainer["Grafana Container\n(port 3000)"]
        JaegerContainer["Jaeger Container\n(port 16686)"]
    end

    Internet["Internet / API Gateway"]
    ExternalSMTP["External SMTP Provider"]
    ExternalSMS["External SMS Provider"]
    ExternalFCM["Firebase Cloud Messaging"]

    Internet --> AppContainer
    AppContainer --> PostgresContainer
    AppContainer --> RabbitMQContainer
    AppContainer --> ExternalSMTP
    AppContainer --> ExternalSMS
    AppContainer --> ExternalFCM
    AppContainer --> Sidecar
    Sidecar --> PrometheusContainer
    PrometheusContainer --> GrafanaContainer
    Sidecar --> JaegerContainer
```

## Deployment Notes

- The service runs as a Docker container on port 8080 (configurable).
- Health checks probe `/health` for liveness and `/metrics` for Prometheus scraping.
- OpenTelemetry exports traces and metrics to a collector sidecar or directly to Jaeger/Prometheus.
- External provider calls (SMTP, SMS, FCM) are made over the public internet.
- RabbitMQ is used for both inbound (upstream events) and outbound (notification events) messaging.
