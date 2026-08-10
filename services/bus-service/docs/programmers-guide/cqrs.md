# Programmer's Guide — CQRS

## Commands (Writes)

| Command | Handler | Description |
|---|---|---|
| `RegisterBusCommand` | `RegisterBusHandler` | Adds a new bus to the fleet |
| `UpdateBusDetailsCommand` | `UpdateBusDetailsHandler` | Updates bus type, seats, depot, manufacturer, model, year |
| `ChangeBusStatusCommand` | `ChangeBusStatusHandler` | Transitions bus status (Active ↔ UnderMaintenance → Retired) |
| `SoftDeleteBusCommand` | `SoftDeleteBusHandler` | Soft-deletes a bus |
| `RestoreBusCommand` | `RestoreBusHandler` | Restores a soft-deleted bus |
| `CreateDepotCommand` | `CreateDepotHandler` | Creates a new depot |
| `SoftDeleteDepotCommand` | `SoftDeleteDepotHandler` | Soft-deletes a depot |
| `RestoreDepotCommand` | `RestoreDepotHandler` | Restores a soft-deleted depot |

## Queries (Reads)

| Query | Handler | Description |
|---|---|---|
| `GetBusQuery` | `GetBusHandler` | Single bus by ID (cached) |
| `GetBusesQuery` | `GetBusesHandler` | Paginated, filterable bus list |
| `GetDepotsQuery` | `GetDepotsHandler` | List depots, optionally filtered by city |

## Pipeline Behaviors

- **ValidationBehavior**: Runs FluentValidation validators before the handler.
- **LoggingBehavior**: Logs command/query name, duration, and success/failure.

## Return Shape

All endpoints return a `Result<T>` envelope:

```json
{
  "success": true,
  "message": "Bus registered successfully.",
  "data": { ... },
  "traceId": "00-abc123-..."
}
```

On failure:

```json
{
  "success": false,
  "message": "One or more validation errors occurred.",
  "errors": [
    { "code": "VALIDATION_ERROR", "field": "plateNumber", "message": "Plate number is required." }
  ],
  "traceId": "00-abc123-..."
}
```
