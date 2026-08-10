# Bus Service — Database Schema

## Schema: `bus`

All tables live in the `bus` schema unless otherwise noted.

### `buses`

| Column | Type | Constraints |
|---|---|---|
| Id | uuid | PK |
| OperatorId | uuid | NOT NULL |
| PlateNumber | varchar(20) | NOT NULL, UNIQUE |
| BusType | varchar(20) | NOT NULL |
| TotalSeats | integer | NOT NULL |
| DepotId | uuid | NOT NULL |
| Status | varchar(20) | NOT NULL |
| Manufacturer | varchar(100) | NULL |
| Model | varchar(100) | NULL |
| YearOfManufacture | integer | NULL |
| TenantId | uuid | NULL |
| CompanyId | uuid | NULL |
| OrganizationId | uuid | NULL |
| CreatedAtUtc | timestamptz | NOT NULL |
| UpdatedAtUtc | timestamptz | NOT NULL |
| Version | integer | NOT NULL, concurrency token (optimistic) |
| IsDeleted | boolean | NOT NULL |
| DeletedAtUtc | timestamptz | NULL |
| DeletedBy | varchar(100) | NULL |

**Indexes**:
- `IX_buses_PlateNumber` (PlateNumber) — unique
- `IX_buses_OperatorId` (OperatorId)
- `IX_buses_DepotId` (DepotId)
- `IX_buses_Status` (Status)
- `IX_buses_IsDeleted` (IsDeleted)
- `IX_buses_TenantId_CompanyId_OrganizationId` (TenantId, CompanyId, OrganizationId)
- `IX_buses_CreatedAtUtc` (CreatedAtUtc)

**Concurrency**: `Version` provides optimistic concurrency.

**Soft delete**: `IsDeleted` flag, global query filter excludes deleted rows.

**Domain events**: `BusRegisteredDomainEvent`, `BusDetailsUpdatedDomainEvent`, `BusStatusChangedDomainEvent`, `BusSoftDeletedDomainEvent`, `BusRestoredDomainEvent`.

---

### `depots`

| Column | Type | Constraints |
|---|---|---|
| Id | uuid | PK |
| Name | varchar(200) | NOT NULL |
| City | varchar(100) | NOT NULL |
| Address | varchar(500) | NULL |
| TenantId | uuid | NULL |
| CompanyId | uuid | NULL |
| OrganizationId | uuid | NULL |
| IsDeleted | boolean | NOT NULL |
| DeletedAtUtc | timestamptz | NULL |
| DeletedBy | varchar(100) | NULL |

**Indexes**:
- `IX_depots_City` (City)
- `IX_depots_IsDeleted` (IsDeleted)
- `IX_depots_TenantId_CompanyId_OrganizationId` (TenantId, CompanyId, OrganizationId)

**Soft delete**: `IsDeleted` flag, global query filter excludes deleted rows.

---

### `audit_logs`

| Column | Type | Constraints |
|---|---|---|
| Id | uuid | PK |
| Action | varchar(100) | NOT NULL |
| EntityName | varchar(100) | NOT NULL |
| EntityId | uuid | NOT NULL |
| UserId | varchar(100) | NOT NULL |
| Changes | text | NULL |
| OccurredOnUtc | timestamptz | NOT NULL |
| IpAddress | varchar(45) | NULL |
| CorrelationId | varchar(100) | NULL |

**Indexes**:
- `IX_audit_logs_EntityName_EntityId` (EntityName, EntityId)
- `IX_audit_logs_OccurredOnUtc` (OccurredOnUtc)

---

### `outbox_messages`

| Column | Type | Constraints |
|---|---|---|
| Id | uuid | PK |
| EventType | varchar(500) | NOT NULL |
| Payload | text | NOT NULL |
| OccurredOnUtc | timestamptz | NOT NULL |
| ProcessedOnUtc | timestamptz | NULL |
| Error | varchar(4000) | NULL |
| RetryCount | integer | NOT NULL |

**Indexes**:
- `IX_outbox_messages_ProcessedOnUtc_RetryCount` (ProcessedOnUtc, RetryCount)

---

## Migrations

```bash
# Add migration
dotnet ef migrations add <Name> --project src/BusService.Infrastructure --startup-project src/BusService.Api

# Apply migrations
dotnet ef database update --project src/BusService.Infrastructure --startup-project src/BusService.Api

# List migrations
dotnet ef migrations list --project src/BusService.Infrastructure --startup-project src/BusService.Api

# Remove last migration (not applied)
dotnet ef migrations remove --project src/BusService.Infrastructure --startup-project src/BusService.Api
```

## Provider Considerations

- **PostgreSQL**: Uses `uuid`, `timestamptz`, `xid` (xmin via `Version`), `boolean`
- **SQL Server**: Uses `uniqueidentifier`, `datetimeoffset`, `rowversion`, `bit`
- **MySQL**: Uses `char(36)` for GUIDs (Pomelo provider default)

Switching providers requires regenerating migrations.
