.NET Services
        │
        ▼
OpenTelemetry SDK
        │
        ▼
OpenTelemetry Collector
      ├────────► Jaeger
      ├────────► Prometheus
      └────────► future exporters

-----------------------------------

update docker-compose like
-------
Infrastructure

├── PostgreSQL
├── RabbitMQ
├── Redis
├── Seq
├── OpenTelemetry Collector
├── Jaeger
├── Prometheus
├── Grafana
├── PgAdmin
├── RedisInsight

Applications

├── BookingService
├── Angular Customer Portal
├── React Admin Portal

-----

add persistant docker-volumes
----

postgres-data
rabbitmq-data
redis-data
seq-data
grafana-data
prometheus-data
pgadmin-data

-----
create dedicated bridge network
----

networks:
  bus-ticketing-network:
    driver: bridge

------
Every container should be attached to that network.

Health Checks

Every infrastructure container should have a health check:

PostgreSQL ✅
RabbitMQ ✅
Redis ✅
Seq ✅
Jaeger ✅
Prometheus ✅
Grafana ✅
OTEL Collector ✅

Then our services can safely use:

------

Recomended Infrastructure
----
infrastructure/
│
├── docker/
│   ├── docker-compose.yml
│   ├── docker-compose.override.yml
│   ├── .env
│   │
│   ├── postgres/
│   │
│   ├── rabbitmq/
│   │
│   ├── redis/
│   │
│   ├── seq/
│   │
│   ├── prometheus/
│   │   └── prometheus.yml
│   │
│   ├── grafana/
│   │   ├── dashboards/
│   │   └── provisioning/
│   │
│   ├── otel/
│   │   └── otel-collector-config.yaml
│   │
│   ├── jaeger/
│   │
│   ├── pgadmin/
│   │
│   └── redisinsight/