-- Stored Procedures for Bus Service (PostgreSQL)
-- Run: psql -U postgres -d bus_service -f scripts/stored-procedures.sql

-- Get paginated buses with filters
CREATE OR REPLACE FUNCTION bus.get_buses(
    p_operator_id uuid DEFAULT NULL,
    p_depot_id uuid DEFAULT NULL,
    p_status varchar DEFAULT NULL,
    p_page integer DEFAULT 1,
    p_page_size integer DEFAULT 50
)
RETURNS TABLE (
    Id uuid,
    OperatorId uuid,
    PlateNumber varchar(20),
    BusType varchar(20),
    TotalSeats integer,
    DepotId uuid,
    Status varchar(20),
    Manufacturer varchar(100),
    Model varchar(100),
    YearOfManufacture integer,
    IsDeleted boolean,
    CreatedAtUtc timestamptz,
    UpdatedAtUtc timestamptz,
    TotalCount bigint
)
LANGUAGE sql
AS $$
    SELECT 
        b.Id, b.OperatorId, b.PlateNumber, b.BusType, b.TotalSeats,
        b.DepotId, b.Status, b.Manufacturer, b.Model, b.YearOfManufacture,
        b.IsDeleted, b.CreatedAtUtc, b.UpdatedAtUtc,
        COUNT(*) OVER() AS TotalCount
    FROM bus.buses b
    WHERE 
        (p_operator_id IS NULL OR b.OperatorId = p_operator_id)
        AND (p_depot_id IS NULL OR b.DepotId = p_depot_id)
        AND (p_status IS NULL OR b.Status = p_status)
        AND b.IsDeleted = false
    ORDER BY b.CreatedAtUtc DESC
    LIMIT p_page_size
    OFFSET (p_page - 1) * p_page_size;
$$;

-- Get depots by city
CREATE OR REPLACE FUNCTION bus.get_depots(p_city varchar DEFAULT NULL)
RETURNS TABLE (
    Id uuid,
    Name varchar(200),
    City varchar(100),
    Address varchar(500),
    IsDeleted boolean
)
LANGUAGE sql
AS $$
    SELECT d.Id, d.Name, d.City, d.Address, d.IsDeleted
    FROM bus.depots d
    WHERE (p_city IS NULL OR d.City ILIKE '%' || p_city || '%')
      AND d.IsDeleted = false
    ORDER BY d.Name;
$$;

-- Soft delete a bus (idempotent)
CREATE OR REPLACE FUNCTION bus.soft_delete_bus(
    p_bus_id uuid,
    p_deleted_by varchar(100)
)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE bus.buses
    SET IsDeleted = true,
        DeletedAtUtc = now(),
        DeletedBy = p_deleted_by,
        Status = 'Retired',
        UpdatedAtUtc = now()
    WHERE Id = p_bus_id AND IsDeleted = false;
END;
$$;

-- Restore a soft-deleted bus
CREATE OR REPLACE FUNCTION bus.restore_bus(p_bus_id uuid)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
    UPDATE bus.buses
    SET IsDeleted = false,
        DeletedAtUtc = NULL,
        DeletedBy = NULL,
        Status = 'Active',
        UpdatedAtUtc = now()
    WHERE Id = p_bus_id AND IsDeleted = true;
END;
$$;

-- Get audit trail for an entity
CREATE OR REPLACE FUNCTION bus.get_audit_trail(p_entity_name varchar, p_entity_id uuid)
RETURNS TABLE (
    Id uuid,
    Action varchar(100),
    EntityName varchar(100),
    EntityId uuid,
    UserId varchar(100),
    Changes text,
    OccurredOnUtc timestamptz,
    IpAddress varchar(45),
    CorrelationId varchar(100)
)
LANGUAGE sql
AS $$
    SELECT * FROM bus.audit_logs
    WHERE EntityName = p_entity_name AND EntityId = p_entity_id
    ORDER BY OccurredOnUtc DESC;
$$;
