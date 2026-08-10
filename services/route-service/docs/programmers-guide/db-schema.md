# DB Schema — Route Service

## Schema Overview

All tables live in the `route` schema.

```sql
CREATE SCHEMA IF NOT EXISTS route;
```

## Tables

### routes

```sql
CREATE TABLE route.routes (
    Id               uuid           NOT NULL PRIMARY KEY,
    Code             varchar(50)    NOT NULL UNIQUE,
    Name             varchar(200)   NOT NULL,
    OriginStopId     uuid           NOT NULL,
    DestinationStopId uuid          NOT NULL,
    TransportMode    varchar(20)    NOT NULL,
    DistanceKm       double precision NOT NULL,
    EstimatedDuration interval       NOT NULL,
    Status           varchar(30)    NOT NULL,
    CreatedBy        text           NULL,
    UpdatedBy        text           NULL,
    CreatedAtUtc     timestamptz    NOT NULL,
    UpdatedAtUtc     timestamptz    NOT NULL,
    IsDeleted        boolean        NOT NULL DEFAULT false,
    DeletedAtUtc     timestamptz    NULL,
    Version          bigint         NOT NULL
);
```

Indexes:
```sql
CREATE UNIQUE INDEX IX_routes_Code ON route.routes (Code);
CREATE INDEX IX_routes_OriginStopId ON route.routes (OriginStopId);
CREATE INDEX IX_routes_DestinationStopId ON route.routes (DestinationStopId);
CREATE INDEX IX_routes_Status ON route.routes (Status);
CREATE INDEX IX_routes_Version ON route.routes (Version);
```

### stops

```sql
CREATE TABLE route.stops (
    Id               uuid           NOT NULL PRIMARY KEY,
    Code             varchar(50)    NOT NULL UNIQUE,
    Name             varchar(200)   NOT NULL,
    City             varchar(100)   NOT NULL,
    Address          varchar(500)   NULL,
    Latitude         double precision NOT NULL,
    Longitude        double precision NOT NULL,
    CreatedBy        text           NULL,
    UpdatedBy        text           NULL,
    CreatedAtUtc     timestamptz    NOT NULL,
    UpdatedAtUtc     timestamptz    NOT NULL,
    IsDeleted        boolean        NOT NULL DEFAULT false,
    DeletedAtUtc     timestamptz    NULL,
    Version          bigint         NOT NULL
);
```

Indexes:
```sql
CREATE UNIQUE INDEX IX_stops_Code ON route.stops (Code);
```

### route_stops

```sql
CREATE TABLE route.route_stops (
    Id                  uuid        NOT NULL PRIMARY KEY,
    RouteId             uuid        NOT NULL,
    StopId              uuid        NOT NULL,
    StopOrder           integer     NOT NULL,
    ArrivalTimeOffset   interval    NULL,
    DepartureTimeOffset interval   NULL,

    CONSTRAINT FK_route_stops_routes_RouteId
        FOREIGN KEY (RouteId) REFERENCES route.routes (Id) ON DELETE CASCADE,
    CONSTRAINT FK_route_stops_stops_StopId
        FOREIGN KEY (StopId) REFERENCES route.stops (Id) ON DELETE RESTRICT
);
```

Indexes:
```sql
CREATE UNIQUE INDEX IX_route_stops_RouteId_StopOrder ON route.route_stops (RouteId, StopOrder);
CREATE INDEX IX_route_stops_StopId ON route.route_stops (StopId);
```

### schedules

```sql
CREATE TABLE route.schedules (
    Id               uuid           NOT NULL PRIMARY KEY,
    RouteId          uuid           NOT NULL,
    DepartureTime    interval       NOT NULL,
    ArrivalTime      interval       NOT NULL,
    Status           varchar(30)    NOT NULL,
    EffectiveFrom    timestamptz    NOT NULL,
    EffectiveTo      timestamptz    NULL,
    CreatedBy        text           NULL,
    UpdatedBy        text           NULL,
    CreatedAtUtc     timestamptz    NOT NULL,
    UpdatedAtUtc     timestamptz    NOT NULL,
    IsDeleted        boolean        NOT NULL DEFAULT false,
    DeletedAtUtc     timestamptz    NULL,
    Version          bigint         NOT NULL,

    CONSTRAINT FK_schedules_routes_RouteId
        FOREIGN KEY (RouteId) REFERENCES route.routes (Id) ON DELETE CASCADE
);
```

Indexes:
```sql
CREATE INDEX IX_schedules_RouteId ON route.schedules (RouteId);
CREATE INDEX IX_schedules_Status ON route.schedules (Status);
CREATE INDEX IX_schedules_Version ON route.schedules (Version);
```

### audit_logs

```sql
CREATE TABLE route.audit_logs (
    Id             uuid           NOT NULL PRIMARY KEY,
    Action         varchar(100)   NOT NULL,
    EntityName     varchar(100)   NOT NULL,
    EntityId       uuid           NOT NULL,
    UserId         varchar(100)   NULL,
    Changes        varchar(4000)  NULL,
    OccurredOnUtc  timestamptz    NOT NULL,
    IpAddress      varchar(45)    NULL,
    CorrelationId  varchar(100)   NULL
);
```

Indexes:
```sql
CREATE INDEX IX_audit_logs_EntityName_EntityId ON route.audit_logs (EntityName, EntityId);
CREATE INDEX IX_audit_logs_OccurredOnUtc ON route.audit_logs (OccurredOnUtc);
```

### outbox_messages

```sql
CREATE TABLE route.outbox_messages (
    Id             uuid           NOT NULL PRIMARY KEY,
    EventType      varchar(500)   NOT NULL,
    Payload        text           NOT NULL,
    OccurredOnUtc  timestamptz    NOT NULL,
    ProcessedOnUtc timestamptz    NULL,
    Error          text           NULL,
    RetryCount     integer        NOT NULL DEFAULT 0
);
```

Indexes:
```sql
CREATE INDEX IX_outbox_messages_ProcessedOnUtc_RetryCount ON route.outbox_messages (ProcessedOnUtc, RetryCount);
```

## Seed Data

```sql
INSERT INTO route.stops (Id, Code, Name, City, Address, Latitude, Longitude, CreatedAtUtc, UpdatedAtUtc, IsDeleted, Version)
VALUES
    ('a1b2c3d4-e5f6-7890-abcd-ef1234567890', 'DHK', 'Dhaka', 'Dhaka', 'Kamalapur', 23.8103, 90.4125, now(), now(), false, 1),
    ('b2c3d4e5-f6a7-8901-bcde-fa2345678901', 'CTG', 'Chittagong', 'Chittagong', 'CTG Station', 22.3569, 91.7832, now(), now(), false, 1),
    ('c3d4e5f6-a7b8-9012-cdef-ab3456789012', 'SYL', 'Sylhet', 'Sylhet', 'Bus Terminal', 24.8949, 91.8687, now(), now(), false, 1)
ON CONFLICT (Code) DO NOTHING;
```

## Triggers

Route Service does not currently use database triggers. All domain logic
lives in the aggregate root methods and is persisted via EF Core
`SaveChangesAsync`. If you need a trigger (e.g. for cross-schema audit),
add it in a new migration:

```sql
CREATE OR REPLACE FUNCTION route.log_audit()
RETURNS TRIGGER AS $$
BEGIN
    INSERT INTO route.audit_logs (...)
    VALUES (...);
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_audit_routes
AFTER INSERT OR UPDATE OR DELETE ON route.routes
FOR EACH ROW EXECUTE FUNCTION route.log_audit();
```

## Functions / Views

No stored functions or views are currently used. If you need a materialized
view for route search optimization, add it in a migration and reference it
via `ToView()` in the entity configuration.
