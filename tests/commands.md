to run postgres in docker 

docker run -d \
  --name bus-ticketing-postgres \
  -e POSTGRES_DB=booking_service \
  -e POSTGRES_USER=booking_svc \
  -e POSTGRES_PASSWORD=changeme \
  -p 5432:5432 \
  postgres:16-alpine

  to run rabbitmq

  docker run -d \
  --name bus-ticketing-rabbitmq \
  -p 5672:5672 \
  -p 15672:15672 \
  rabbitmq:3.13-management-alpine

  to run redis
  
  docker run -d \
  --name bus-ticketing-redis \
  --restart unless-stopped \
  -p 6379:6379 \
  -v redis-data:/data \
  redis:7-alpine \
  redis-server --appendonly yes

  to run seq (Structured Log Viewer)

  docker run -d \
  --name bus-ticketing-seq \
  --restart unless-stopped \
  -e ACCEPT_EULA=Y \
  -p 5341:80 \
  -v seq-data:/data \
  datalust/seq:latest

  to run grafana

  docker run -d \
  --name bus-ticketing-grafana \
  --restart unless-stopped \
  -p 3000:3000 \
  -v grafana-data:/var/lib/grafana \
  grafana/grafana

  to run jeager

  docker run -d \
  --name bus-ticketing-jaeger \
  --restart unless-stopped \
  -p 16686:16686 \
  -p 4317:4317 \
  -p 4318:4318 \
  jaegertracing/all-in-one:latest

