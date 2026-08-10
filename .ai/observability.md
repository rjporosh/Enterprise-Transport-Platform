# Exception Log Standard

## 1. Purpose

The platform must maintain a dedicated exception log so developers can identify and fix production problems as quickly as possible.

Exception logging must not merely record:

```text
Exception occurred.
```

It must provide enough structured information to answer:

```text
Where did it happen?
What endpoint/job triggered it?
Which method failed?
Which file contains the failure?
Which line failed?
What was the exact exception?
What caused it?
How can it be fixed?
What is the recommended engineering practice?
```

---

# 2. Exception Log Location

Every service must maintain:

```text
logs/
└── exception-logs/
    └── exception-logs-dd-MM-yyyy.txt
```

Example:

```text
logs/exception-logs/exception-logs-07-08-2026.txt
```

The filename must use the service/server local logging convention consistently.

---

# 3. Exception Log vs Other Logs

The platform maintains separate diagnostic categories.

```text
logs/
├── runtime-error-logs/
├── build-error-logs/
├── query-logs/
└── exception-logs/
```

### Runtime Error Logs

Used for runtime/dependency/environment failures.

Examples:

```text
Database unavailable
Redis unavailable
RabbitMQ unavailable
External service unavailable
Configuration failure
Connection failure
```

### Build Error Logs

Used for:

```text
Compilation errors
Build failures
Package failures
Configuration/build-tool failures
```

### Query Logs

Used for:

```text
Database queries
Query execution time
Database provider
Endpoint
Method
Repository
```

### Exception Logs

Used for application exceptions and centralized exception diagnostics.

---

# 4. Centralized Exception Handling

All unhandled application exceptions must pass through centralized exception handling.

Preferred flow:

```text
Request
   ↓
Endpoint
   ↓
Application
   ↓
Domain/Infrastructure
   ↓
Exception
   ↓
Global Exception Handler
   ↓
Exception Log
   ↓
Structured API Error Response
```

Do not implement independent exception formatting in every controller.

---

# 5. Required Exception Log Information

Every exception entry should contain, where available:

```text
Timestamp
Service Name
Environment
Entry Point
Endpoint Name
HTTP Method
Request Path
Application Method Name
Class Name
File Name
File Location
Line Number
CorrelationId
TraceId
TenantId
CompanyId
OrganizationId
UserId where appropriate
Exception Type
Exact Exception Message
Stack Trace
Inner Exception
Root Cause
Possible Solution
Best Practice Recommendation
```

---

# 6. Entry Point

The log must identify where execution entered the application.

Examples:

```text
EntryPoint: HTTP Request
EntryPoint: gRPC Request
EntryPoint: RabbitMQ Consumer
EntryPoint: Quartz Job
EntryPoint: Background Worker
EntryPoint: Scheduled Task
```

This allows developers to immediately distinguish API failures from background-processing failures.

---

# 7. Endpoint Name

For HTTP requests:

```text
EndpointName: NotificationController.Send
```

or the actual endpoint identifier used by the framework.

For non-HTTP execution:

```text
EndpointName: N/A
```

and the actual job/consumer name should be recorded.

---

# 8. Background Service / Quartz Identification

For background execution record:

```text
EntryPoint: Quartz Job
JobName: NotificationRetryJob
TriggerName: NotificationRetryTrigger
```

For a consumer:

```text
EntryPoint: RabbitMQ Consumer
ConsumerName: NotificationRequestedConsumer
QueueName: notification.requested
```

This allows developers to immediately identify which background component failed.

---

# 9. Method Information

Record:

```text
ClassName
MethodName
MethodSignature where practical
```

Example:

```text
ClassName: NotificationCommandHandler
MethodName: Handle
```

---

# 10. Source File Information

Record:

```text
FileName
FileLocation
LineNumber
```

Example:

```text
FileName: NotificationCommandHandler.cs
FileLocation: src/Notification.Application/Notifications/Commands/SendNotificationCommandHandler.cs
LineNumber: 87
```

The information should identify the most useful application source location rather than only framework internals.

---

# 11. Exact Exception

Always preserve the exact technical exception information in the server-side log.

Example:

```text
ExceptionType: NpgsqlException

ExactExceptionMessage:
42P01: relation "notifications" does not exist
```

Do not replace the technical exception with only:

```text
Something went wrong.
```

The technical details belong in logs, not in the public API response.

---

# 12. Root Cause

The log must attempt to identify the actual root cause.

Bad:

```text
RootCause:
Database error
```

Better:

```text
RootCause:
The Notifications table does not exist in the configured PostgreSQL
database. The application attempted to execute a query against the
missing table.
```

The root cause must be based on evidence from the actual exception and execution context.

Never invent a root cause.

---

# 13. Possible Solution

Provide a practical solution when it can be determined.

Example:

```text
PossibleSolution:
Verify that the latest EF Core migration has been applied to the
configured PostgreSQL database.

Run the service's documented database migration command from the
repository root.
```

If the solution cannot be determined automatically:

```text
PossibleSolution:
Investigate the inner exception and verify database schema,
connection configuration, and migration state.
```

---

# 14. Best Practice Recommendation

Where useful, provide an engineering recommendation.

Example:

```text
BestPractice:
Ensure database migrations are executed during deployment or through
the approved deployment pipeline. Do not manually modify production
schema.
```

Another example:

```text
BestPractice:
Use resilient database connectivity and health checks, but do not
blindly retry non-transient database exceptions.
```

---

# 15. Structured Entry Format

Preferred human-readable structure:

```text
============================================================
EXCEPTION
============================================================

Timestamp:
2026-08-07T10:25:31.482Z

Service:
Notification

Environment:
Development

EntryPoint:
HTTP Request

EndpointName:
NotificationController.Send

RequestPath:
/api/notifications/send

HttpMethod:
POST

ClassName:
SendNotificationCommandHandler

MethodName:
Handle

FileName:
SendNotificationCommandHandler.cs

FileLocation:
src/Notification.Application/Notifications/Commands/
SendNotificationCommandHandler.cs

LineNumber:
87

CorrelationId:
xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx

TraceId:
xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx

TenantId:
xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx

CompanyId:
xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx

OrganizationId:
xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx

ExceptionType:
NpgsqlException

ExactExceptionMessage:
42P01: relation "notifications" does not exist

RootCause:
The Notifications table does not exist in the configured database.

PossibleSolution:
Verify the configured database and apply the latest EF Core migration.

BestPractice:
Database schema changes should be applied through the approved
migration/deployment process.

StackTrace:
<full server-side stack trace>

InnerException:
<inner exception if available>

============================================================
```

---

# 16. Sensitive Data Protection

Exception logs must NEVER expose secrets.

Do not log:

```text
Passwords
Password hashes
OTP values
Access tokens
Refresh tokens
API secrets
Encryption keys
Credit card numbers
CVV
Private keys
Connection-string passwords
```

Request/response bodies must be filtered and masked where necessary.

---

# 17. Tenant Safety

If the platform is multi-tenant, exception logs should include tenant context when available:

```text
TenantId
CompanyId
OrganizationId
```

This allows operational troubleshooting without mixing tenant context.

Do not expose tenant-sensitive information to unauthorized users.

---

# 18. Correlation and Distributed Tracing

Every exception should include:

```text
CorrelationId
TraceId
```

when available.

This allows developers to follow:

```text
Frontend
 ↓
Gateway
 ↓
Service A
 ↓
gRPC
 ↓
Service B
 ↓
RabbitMQ
 ↓
Service C
 ↓
Exception
```

using a single trace/correlation context.

---

# 19. API Response vs Exception Log

The API must NOT return the complete exception log to the client.

Client:

```json
{
  "success": false,
  "message": "The requested operation could not be completed.",
  "errors": [
    {
      "code": "INTERNAL_ERROR",
      "message": "An unexpected error occurred."
    }
  ],
  "traceId": "..."
}
```

Server log:

```text
ExactExceptionMessage
RootCause
StackTrace
File
Line
PossibleSolution
BestPractice
```

This separation protects internal implementation details while keeping debugging information available to developers.

---

# 20. Exception Classification

Where practical, classify exceptions.

Examples:

```text
ValidationException
AuthenticationException
AuthorizationException
NotFoundException
ConflictException
ConcurrencyException
DatabaseException
ExternalServiceException
TimeoutException
MessagingException
InfrastructureException
UnhandledException
```

Classification should map to the platform's centralized Result/Error model.

---

# 21. Expected vs Unexpected Exceptions

Not every exception is a system failure.

### Expected

Examples:

```text
Validation
Not Found
Conflict
Unauthorized
Forbidden
Business Rule Violation
```

These should be handled predictably.

### Unexpected

Examples:

```text
NullReferenceException
Database corruption
Unexpected infrastructure failure
Unhandled dependency failure
Programming error
```

These require detailed exception logging.

---

# 22. Exception Logging Pipeline

Recommended:

```text
Exception
   ↓
Global Exception Handler
   ↓
Exception Classification
   ↓
Structured Exception Log
   ↓
Correlation/Trace Context
   ↓
Centralized Error Result
   ↓
Client Response
```

For centralized observability:

```text
Exception Log
      ↓
Serilog
      ↓
OpenTelemetry
      ↓
Seq / Grafana / Kibana / Graylog
```

depending on the configured environment.

---

# 23. File Logging and Centralized Logging

Local file logging is mandatory for immediate developer troubleshooting where enabled:

```text
logs/exception-logs/
```

Centralized logging should additionally be supported where configured.

Possible destinations:

```text
Seq
Elasticsearch/Kibana
Graylog
OpenTelemetry-compatible systems
```

The local file should not be considered a replacement for centralized production observability.

---

# 24. Log Rotation

Exception logs must be rotated by date.

Example:

```text
exception-logs-06-08-2026.txt
exception-logs-07-08-2026.txt
exception-logs-08-08-2026.txt
```

Retention must be configurable.

Production deployments must prevent unlimited disk growth.

---

# 25. Exception Metrics

Track at least:

```text
Exceptions Total
Exceptions by Service
Exceptions by Endpoint
Exceptions by Exception Type
Exceptions by HTTP Status
Exceptions by Tenant where appropriate
```

Recommended metrics:

```text
exception.count
exception.duration
exception.unhandled
```

Use OpenTelemetry metrics where supported.

---

# 26. Developer Troubleshooting Workflow

When an exception occurs:

```text
1. Get TraceId / CorrelationId
        ↓
2. Search exception logs
        ↓
3. Identify EntryPoint
        ↓
4. Identify Endpoint / Job / Consumer
        ↓
5. Identify Method
        ↓
6. Open FileLocation
        ↓
7. Check LineNumber
        ↓
8. Read ExactExceptionMessage
        ↓
9. Check RootCause
        ↓
10. Apply PossibleSolution
        ↓
11. Review BestPractice
        ↓
12. Rebuild
        ↓
13. Test
```

The logging system is explicitly designed to minimize developer diagnosis time.

---

# 27. Developer-Speed Principle

The exception log should answer these five questions immediately:

```text
WHAT failed?
WHERE did it fail?
WHY did it fail?
HOW can it be fixed?
WHAT is the better engineering practice?
```

If the log does not provide enough information to answer these questions, improve the logging implementation.

---

# 28. Final Exception Logging Checklist

```text
[ ] Centralized exception handler
[ ] Dedicated exception log directory
[ ] Daily exception log file
[ ] Timestamp
[ ] Service name
[ ] Environment
[ ] Entry point
[ ] Endpoint name
[ ] HTTP method
[ ] Request path
[ ] Job/consumer name where applicable
[ ] Class name
[ ] Method name
[ ] File name
[ ] File location
[ ] Line number
[ ] CorrelationId
[ ] TraceId
[ ] TenantId where applicable
[ ] CompanyId where applicable
[ ] OrganizationId where applicable
[ ] Exception type
[ ] Exact exception message
[ ] Stack trace
[ ] Inner exception
[ ] Root cause
[ ] Possible solution
[ ] Best practice
[ ] Sensitive-data masking
[ ] Structured logging
[ ] Centralized logging integration
[ ] Log rotation
[ ] Exception metrics
[ ] Developer troubleshooting documentation
```

# END OF EXCEPTION LOG STANDARD
