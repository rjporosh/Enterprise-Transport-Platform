1. PostgreSQL
docker run -d \
  --name bus-ticketing-postgres \
  --restart unless-stopped \
  -e POSTGRES_DB=booking_service \
  -e POSTGRES_USER=booking_svc \
  -e POSTGRES_PASSWORD=changeme \
  -p 5432:5432 \
  -v postgres-data:/var/lib/postgresql/data \
  postgres:16-alpine
2. RabbitMQ
docker run -d \
  --name bus-ticketing-rabbitmq \
  --restart unless-stopped \
  -p 5672:5672 \
  -p 15672:15672 \
  rabbitmq:3.13-management-alpine

RabbitMQ UI

http://localhost:15672

Login

guest
guest
3. Redis
docker run -d \
  --name bus-ticketing-redis \
  --restart unless-stopped \
  -p 6379:6379 \
  -v redis-data:/data \
  redis:7-alpine \
  redis-server --appendonly yes

Test

docker exec -it bus-ticketing-redis redis-cli

then

PING

should return

PONG
4. Seq (Structured Log Viewer)
docker run -d \
  --name bus-ticketing-seq \
  --restart unless-stopped \
  -e ACCEPT_EULA=Y \
  -p 5341:80 \
  -v seq-data:/data \
  datalust/seq:latest

Open

http://localhost:5341

No login required initially.

5. Prometheus

Create a configuration folder

mkdir -p ~/monitoring/prometheus

Create

prometheus.yml

inside it.

Example:

global:
  scrape_interval: 15s

scrape_configs:
  - job_name: "booking-service"
    static_configs:
      - targets:
          - host.docker.internal:5000

Run

docker run -d \
  --name bus-ticketing-prometheus \
  --restart unless-stopped \
  -p 9090:9090 \
  -v ~/monitoring/prometheus/prometheus.yml:/etc/prometheus/prometheus.yml \
  prom/prometheus

Open

http://localhost:9090
6. Grafana
docker run -d \
  --name bus-ticketing-grafana \
  --restart unless-stopped \
  -p 3000:3000 \
  -v grafana-data:/var/lib/grafana \
  grafana/grafana

Open

http://localhost:3000

Default login

admin
admin

It will ask you to change the password.

7. Jaeger (OpenTelemetry)
docker run -d \
  --name bus-ticketing-jaeger \
  --restart unless-stopped \
  -p 16686:16686 \
  -p 4317:4317 \
  -p 4318:4318 \
  jaegertracing/all-in-one:latest

Open

http://localhost:16686
Verify Everything
docker ps

Expected containers

bus-ticketing-postgres
bus-ticketing-rabbitmq
bus-ticketing-redis
bus-ticketing-seq
bus-ticketing-prometheus
bus-ticketing-grafana
bus-ticketing-jaeger
Enterprise Local Development Stack
Service	Port	Purpose
PostgreSQL	5432	Primary database
RabbitMQ	5672	Message broker
RabbitMQ UI	15672	Queue management
Redis	6379	Distributed cache, locking, sessions
Seq	5341	Structured log aggregation
Prometheus	9090	Metrics collection
Grafana	3000	Dashboards and visualization
Jaeger UI	16686	Distributed tracing
OTLP gRPC	4317	OpenTelemetry exporter
OTLP HTTP	4318	OpenTelemetry exporter

This stack is an excellent foundation for a production-style .NET microservices development environment. Once you've verified each service individually, the next step is to consolidate them into a single docker-compose.yml so the entire platform starts with one command:

docker compose up -d

That gives you a repeatable, one-command local infrastructure that closely mirrors how the services would be orchestrated in larger environments.