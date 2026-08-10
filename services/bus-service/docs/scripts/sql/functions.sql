-- Functions for Bus Service (PostgreSQL)
-- Run: psql -U postgres -d bus_service -f scripts/functions.sql

-- Check if plate number exists (for validation)
CREATE OR REPLACE FUNCTION bus.plate_number_exists(p_plate_number varchar)
RETURNS boolean
LANGUAGE sql
STABLE
AS $$
    SELECT EXISTS (
        SELECT 1 FROM bus.buses 
        WHERE PlateNumber = UPPER(TRIM(p_plate_number)) AND IsDeleted = false
    );
$$;

-- Count active buses per depot
CREATE OR REPLACE FUNCTION bus.count_active_buses_per_depot()
RETURNS TABLE (DepotId uuid, DepotName varchar(200), ActiveBusCount bigint)
LANGUAGE sql
AS $$
    SELECT d.Id, d.Name, COUNT(b.Id)
    FROM bus.depots d
    LEFT JOIN bus.buses b ON b.DepotId = d.Id AND b.Status = 'Active' AND b.IsDeleted = false
    WHERE d.IsDeleted = false
    GROUP BY d.Id, d.Name
    ORDER BY ActiveBusCount DESC;
$$;

-- Get bus status distribution
CREATE OR REPLACE FUNCTION bus.get_bus_status_distribution()
RETURNS TABLE (Status varchar(20), Count bigint)
LANGUAGE sql
AS $$
    SELECT Status, COUNT(*) 
    FROM bus.buses 
    WHERE IsDeleted = false 
    GROUP BY Status;
$$;

-- Is bus eligible for retirement (no active trips)?
CREATE OR REPLACE FUNCTION bus.is_bus_eligible_for_retirement(p_bus_id uuid)
RETURNS boolean
LANGUAGE sql
STABLE
AS $$
    SELECT NOT EXISTS (
        SELECT 1 FROM bus.buses b
        WHERE b.Id = p_bus_id AND b.Status = 'Active' AND b.IsDeleted = false
    );
$$;
