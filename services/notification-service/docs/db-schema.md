# Notification Service — Database Schema

## Schema: `notification`

All tables live in the `notification` schema.

### `notifications`

| Column | Type | Constraints |
|---|---|---|
| Id | uuid | PK |
| Recipient | varchar(320) | NOT NULL |
| Channel | varchar(20) | NOT NULL |
| Status | varchar(20) | NOT NULL |
| Priority | varchar(20) | NOT NULL |
| Subject | varchar(500) | NULL |
| Body | text | NOT NULL |
| DataPayload | text | NULL |
| TemplateId | uuid | NULL |
| SourceReference | varchar(200) | NULL |
| Locale | varchar(10) | NULL |
| ScheduledForUtc | timestamp with time zone | NULL |
| SentAtUtc | timestamp with time zone | NULL |
| DeliveredAtUtc | timestamp with time zone | NULL |
| RetryCount | integer | NOT NULL |
| MaxRetryCount | integer | NOT NULL |
| NextRetryAtUtc | timestamp with time zone | NULL |
| LastError | varchar(4000) | NULL |
| IsDeleted | boolean | NOT NULL |
| CreatedAtUtc | timestamp with time zone | NOT NULL |
| UpdatedAtUtc | timestamp with time zone | NULL |
| xmin | xid | rowversion, NOT NULL |

**Indexes**:
- `IX_notifications_CreatedAtUtc` (CreatedAtUtc)
- `IX_notifications_Recipient` (Recipient)
- `IX_notifications_SourceReference` (SourceReference)
- `IX_notifications_Status` (Status)
- `IX_notifications_Status_NextRetryAtUtc` (Status, NextRetryAtUtc)
- `IX_notifications_Status_ScheduledForUtc` (Status, ScheduledForUtc)

**Concurrency**: `xmin` provides optimistic concurrency (Postgres-specific).

**Soft delete**: `IsDeleted` flag, global query filter excludes deleted rows.

### `notification_templates`

| Column | Type | Constraints |
|---|---|---|
| Id | uuid | PK |
| Key | varchar(200) | NOT NULL |
| Channel | varchar(20) | NOT NULL |
| Locale | varchar(10) | NOT NULL |
| Name | varchar(200) | NOT NULL |
| Description | varchar(1000) | NULL |
| Subject | varchar(500) | NULL |
| Body | text | NOT NULL |
| DataPayloadTemplate | text | NULL |
| IsActive | boolean | NOT NULL |
| Version | integer | NOT NULL |
| IsDeleted | boolean | NOT NULL |
| CreatedAtUtc | timestamp with time zone | NOT NULL |
| UpdatedAtUtc | timestamp with time zone | NULL |
| RowVersion | bytea | rowversion, NOT NULL |

**Unique index**: `IX_notification_templates_Key_Channel_Locale` (Key, Channel, Locale)

**Concurrency**: `RowVersion` (bytea) provides optimistic concurrency.

### `notification_logs`

| Column | Type | Constraints |
|---|---|---|
| Id | uuid | PK |
| NotificationId | uuid | FK → notifications.Id (CASCADE) |
| AttemptNumber | integer | NOT NULL |
| WasSuccessful | boolean | NOT NULL |
| ProviderMessageId | varchar(200) | NULL |
| Error | varchar(4000) | NULL |
| AttemptedAtUtc | timestamp with time zone | NOT NULL |

**Index**: `IX_notification_logs_NotificationId` (NotificationId)

### `recipient_preferences`

| Column | Type | Constraints |
|---|---|---|
| Id | uuid | PK |
| RecipientId | varchar(200) | NOT NULL, UNIQUE |
| EmailOptOut | boolean | NOT NULL |
| SmsOptOut | boolean | NOT NULL |
| PushOptOut | boolean | NOT NULL |
| Locale | varchar(10) | NOT NULL |
| CreatedAtUtc | timestamp with time zone | NOT NULL |
| UpdatedAtUtc | timestamp with time zone | NULL |
| Version | bigint | NOT NULL |

**Index**: `IX_recipient_preferences_RecipientId` (RecipientId, unique)

### `outbox_messages`

| Column | Type | Constraints |
|---|---|---|
| Id | uuid | PK |
| EventType | varchar(500) | NOT NULL |
| Payload | text | NOT NULL |
| OccurredOnUtc | timestamp with time zone | NOT NULL |
| ProcessedOnUtc | timestamp with time zone | NULL |
| Error | varchar(4000) | NULL |
| RetryCount | integer | NOT NULL |

**Index**: `IX_outbox_messages_ProcessedOnUtc_RetryCount` (ProcessedOnUtc, RetryCount)

## Migrations

```bash
# Add migration
dotnet ef migrations add <Name> --project src/NotificationService.Infrastructure --startup-project src/NotificationService.Api

# Apply migrations
dotnet ef database update --project src/NotificationService.Infrastructure --startup-project src/NotificationService.Api

# List migrations
dotnet ef migrations list --project src/NotificationService.Infrastructure --startup-project src/NotificationService.Api

# Remove last migration (not applied)
dotnet ef migrations remove --project src/NotificationService.Infrastructure --startup-project src/NotificationService.Api
```

## Provider Considerations

- **PostgreSQL**: Uses `uuid`, `timestamp with time zone`, `xid` (xmin), `bytea` (rowversion)
- **SQL Server**: Uses `uniqueidentifier`, `datetimeoffset`, `rowversion`
- **MySQL**: Uses `char(36)` for GUIDs (Pomelo provider default)

Switching providers requires regenerating migrations.
