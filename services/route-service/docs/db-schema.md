# Route Service — Database Schema

## Tables

### routes
| Column | Type | Notes |
|--------|------|-------|
| id | uuid | PK |
| code | varchar(50) | Unique, not null |
| name | varchar(200) | Not null |
| origin_stop_id | uuid | FK to stops.id |
| destination_stop_id | uuid | FK to stops.id |
| transport_mode | varchar(20) | Enum string |
| distance_km | double precision | |
| estimated_duration | interval | |
| status | varchar(30) | Enum string |
| version | integer | Concurrency token |
| created_by | varchar(100) | |
| updated_by | varchar(100) | |
| created_at_utc | timestamptz | |
| updated_at_utc | timestamptz | |
| is_deleted | boolean | Soft delete flag |
| deleted_at_utc | timestamptz | |

### stops
| Column | Type | Notes |
|--------|------|-------|
| id | uuid | PK |
| code | varchar(50) | Unique, not null |
| name | varchar(200) | Not null |
| city | varchar(100) | Not null |
| address | varchar(500) | |
| latitude | double precision | |
| longitude | double precision | |
| created_by | varchar(100) | |
| updated_by | varchar(100) | |
| created_at_utc | timestamptz | |
| updated_at_utc | timestamptz | |
| is_deleted | boolean | |

### route_stops
| Column | Type | Notes |
|--------|------|-------|
| id | uuid | PK |
| route_id | uuid | FK to routes.id |
| stop_id | uuid | FK to stops.id |
| stop_order | integer | Not null |
| arrival_time_offset | time | |
| departure_time_offset | time | |

### schedules
| Column | Type | Notes |
|--------|------|-------|
| id | uuid | PK |
| route_id | uuid | FK to routes.id |
| departure_time | time | Not null |
| arrival_time | time | Not null |
| status | varchar(30) | Enum string |
| effective_from | timestamptz | Not null |
| effective_to | timestamptz | |
| version | integer | Concurrency token |
| created_by | varchar(100) | |
| updated_by | varchar(100) | |
| created_at_utc | timestamptz | |
| updated_at_utc | timestamptz | |
| is_deleted | boolean | |

### outbox_messages
| Column | Type | Notes |
|--------|------|-------|
| id | uuid | PK |
| event_type | varchar(500) | |
| payload | jsonb | |
| occurred_on_utc | timestamptz | |
| processed_on_utc | timestamptz | |
| error | varchar(1000) | |
| retry_count | integer | |

### audit_logs
| Column | Type | Notes |
|--------|------|-------|
| id | uuid | PK |
| action | varchar(100) | |
| entity_name | varchar(100) | |
| entity_id | uuid | |
| user_id | varchar(100) | |
| changes | varchar(4000) | |
| occurred_on_utc | timestamptz | |
| ip_address | varchar(45) | |
| correlation_id | varchar(100) | |

## Indexes

- `routes.code` (unique)
- `stops.code` (unique)
- `routes.origin_stop_id`, `routes.destination_stop_id`, `routes.status`
- `schedules.route_id`, `schedules.status`
- `route_stops(route_id, stop_order)` (unique)
- `audit_logs(entity_name, entity_id)`, `audit_logs(occurred_on_utc)`
