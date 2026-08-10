-- Bus Service — PostgreSQL Schema
-- Run: psql -U postgres -d bus_service -f scripts/schema.sql

CREATE SCHEMA IF NOT EXISTS bus;

-- Buses
CREATE TABLE IF NOT EXISTS bus.buses (
    Id uuid PRIMARY KEY,
    OperatorId uuid NOT NULL,
    PlateNumber varchar(20) NOT NULL UNIQUE,
    BusType varchar(20) NOT NULL,
    TotalSeats integer NOT NULL,
    DepotId uuid NOT NULL,
    Status varchar(20) NOT NULL,
    Manufacturer varchar(100) NULL,
    Model varchar(100) NULL,
    YearOfManufacture integer NULL,
    TenantId uuid NULL,
    CompanyId uuid NULL,
    OrganizationId uuid NULL,
    CreatedAtUtc timestamptz NOT NULL DEFAULT now(),
    UpdatedAtUtc timestamptz NOT NULL DEFAULT now(),
    Version integer NOT NULL DEFAULT 1,
    IsDeleted boolean NOT NULL DEFAULT false,
    DeletedAtUtc timestamptz NULL,
    DeletedBy varchar(100) NULL
);

CREATE INDEX IF NOT EXISTS IX_buses_OperatorId ON bus.buses(OperatorId);
CREATE INDEX IF NOT EXISTS IX_buses_DepotId ON bus.buses(DepotId);
CREATE INDEX IF NOT EXISTS IX_buses_Status ON bus.buses(Status);
CREATE INDEX IF NOT EXISTS IX_buses_IsDeleted ON bus.buses(IsDeleted);
CREATE INDEX IF NOT EXISTS IX_buses_TenantId_CompanyId_OrganizationId ON bus.buses(TenantId, CompanyId, OrganizationId);
CREATE INDEX IF NOT EXISTS IX_buses_CreatedAtUtc ON bus.buses(CreatedAtUtc);

-- Depots
CREATE TABLE IF NOT EXISTS bus.depots (
    Id uuid PRIMARY KEY,
    Name varchar(200) NOT NULL,
    City varchar(100) NOT NULL,
    Address varchar(500) NULL,
    TenantId uuid NULL,
    CompanyId uuid NULL,
    OrganizationId uuid NULL,
    IsDeleted boolean NOT NULL DEFAULT false,
    DeletedAtUtc timestamptz NULL,
    DeletedBy varchar(100) NULL
);

CREATE INDEX IF NOT EXISTS IX_depots_City ON bus.depots(City);
CREATE INDEX IF NOT EXISTS IX_depots_IsDeleted ON bus.depots(IsDeleted);
CREATE INDEX IF NOT EXISTS IX_depots_TenantId_CompanyId_OrganizationId ON bus.depots(TenantId, CompanyId, OrganizationId);

-- Audit Logs
CREATE TABLE IF NOT EXISTS bus.audit_logs (
    Id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    Action varchar(100) NOT NULL,
    EntityName varchar(100) NOT NULL,
    EntityId uuid NOT NULL,
    UserId varchar(100) NOT NULL,
    Changes text NULL,
    OccurredOnUtc timestamptz NOT NULL DEFAULT now(),
    IpAddress varchar(45) NULL,
    CorrelationId varchar(100) NULL
);

CREATE INDEX IF NOT EXISTS IX_audit_logs_EntityName_EntityId ON bus.audit_logs(EntityName, EntityId);
CREATE INDEX IF NOT EXISTS IX_audit_logs_OccurredOnUtc ON bus.audit_logs(OccurredOnUtc);

-- Outbox Messages
CREATE TABLE IF NOT EXISTS bus.outbox_messages (
    Id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    EventType varchar(500) NOT NULL,
    Payload text NOT NULL,
    OccurredOnUtc timestamptz NOT NULL DEFAULT now(),
    ProcessedOnUtc timestamptz NULL,
    Error varchar(4000) NULL,
    RetryCount integer NOT NULL DEFAULT 0
);

CREATE INDEX IF NOT EXISTS IX_outbox_messages_ProcessedOnUtc_RetryCount ON bus.outbox_messages(ProcessedOnUtc, RetryCount);
