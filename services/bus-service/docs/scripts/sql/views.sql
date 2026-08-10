-- Views for Bus Service (PostgreSQL)
-- Run: psql -U postgres -d bus_service -f scripts/views.sql

-- Active fleet overview
CREATE OR REPLACE VIEW bus.vw_active_fleet AS
SELECT 
    b.Id,
    b.PlateNumber,
    b.BusType,
    b.TotalSeats,
    b.Manufacturer,
    b.Model,
    b.Status,
    d.Name AS DepotName,
    d.City AS DepotCity,
    b.CreatedAtUtc,
    b.UpdatedAtUtc
FROM bus.buses b
JOIN bus.depots d ON d.Id = b.DepotId
WHERE b.IsDeleted = false AND d.IsDeleted = false;

-- Bus status summary
CREATE OR REPLACE VIEW bus.vw_bus_status_summary AS
SELECT 
    Status,
    COUNT(*) AS Count,
    MIN(CreatedAtUtc) AS FirstRegistered,
    MAX(UpdatedAtUtc) AS LastUpdated
FROM bus.buses
WHERE IsDeleted = false
GROUP BY Status;

-- Depot utilization
CREATE OR REPLACE VIEW bus.vw_depot_utilization AS
SELECT 
    d.Id AS DepotId,
    d.Name AS DepotName,
    d.City,
    COUNT(b.Id) FILTER (WHERE b.Status = 'Active') AS ActiveBuses,
    COUNT(b.Id) FILTER (WHERE b.Status = 'UnderMaintenance') AS UnderMaintenanceBuses,
    COUNT(b.Id) FILTER (WHERE b.Status = 'Retired') AS RetiredBuses,
    COUNT(b.Id) AS TotalBuses
FROM bus.depots d
LEFT JOIN bus.buses b ON b.DepotId = d.Id AND b.IsDeleted = false
WHERE d.IsDeleted = false
GROUP BY d.Id, d.Name, d.City;

-- Recent audit log summary
CREATE OR REPLACE VIEW bus.vw_audit_summary AS
SELECT 
    EntityName,
    EntityId,
    Action,
    UserId,
    Count(*) AS ChangeCount,
    MAX(OccurredOnUtc) AS LastChange
FROM bus.audit_logs
GROUP BY EntityName, EntityId, Action, UserId
ORDER BY LastChange DESC;
