# Notification Service — Enterprise Requirements

## 1. Purpose

The Notification Service is a reusable, production-grade notification platform.

It must be suitable for:

* Transport systems
* Booking systems
* Payment systems
* Payment gateways
* HRM
* ERP
* Accounting
* SaaS
* Healthcare systems
* E-commerce
* Ticketing
* Payroll
* Enterprise applications

The service must be designed as a reusable platform rather than as a project-specific notification module.

---

# 2. Responsibility Boundary

Notification Service owns notification delivery and notification orchestration.

It is responsible for:

* Notification templates
* Notification channels
* Notification preferences
* Notification delivery
* Notification scheduling
* Notification history
* Delivery status
* Retry
* Dead-letter handling
* Provider abstraction
* Notification events
* Notification audit
* Background processing
* Outbox/inbox where required

It must NOT own:

* Authentication
* Payment
* Booking
* Bus
* Route
* User identity
* Business-specific domain logic

Other services request or publish notification intents.

---

# 3. Supported Channels

The architecture must support:

* Email
* SMS
* Push notification
* In-app notification

The provider implementation must be replaceable.

Examples:

```text
Email
 ├── SMTP
 ├── SendGrid
 ├── Amazon SES
 └── Other provider

SMS
 ├── Provider A
 ├── Provider B
 └── Provider C

Push
 ├── Firebase
 ├── APNs
 └── Other provider
```

Do not couple business logic directly to a provider SDK.

---

# 4. Provider Abstraction

Use provider abstractions.

Examples:

```text
IEmailProvider
ISmsProvider
IPushProvider
```

Provider selection must be configuration-driven.

Example:

```text
Notification:EmailProvider=Smtp
Notification:SmsProvider=ProviderA
Notification:PushProvider=Firebase
```

Changing provider configuration must not require changing business logic.

---

# 5. Notification Model

A notification should support:

* NotificationId
* TenantId
* CompanyId
* OrganizationId
* RecipientId/reference
* RecipientAddress
* Channel
* TemplateId
* TemplateVersion
* Subject where applicable
* Payload
* Priority
* Status
* ScheduledAt
* SentAt
* DeliveredAt where available
* FailedAt
* RetryCount
* Provider
* ProviderMessageId
* CorrelationId
* TraceId
* CreatedAt
* UpdatedAt

Do not expose internal persistence entities directly through APIs.

---

# 6. SaaS Isolation

Notification data must support:

* TenantId
* CompanyId
* OrganizationId

Every query and mutation must enforce the correct scope.

Never allow:

```text
Tenant A → Tenant B notification access
```

Cache keys, events, logs, and background jobs must preserve tenant context where applicable.

---

# 7. Notification Templates

Implement reusable templates.

A template should support:

* TemplateId
* TenantId
* CompanyId
* OrganizationId
* Name
* Code
* Channel
* Subject
* Body
* Language
* Version
* Status
* Variables
* CreatedAt
* UpdatedAt

Example:

```text
PAYMENT_SUCCESS
BOOKING_CONFIRMED
PASSWORD_RESET
OTP
BUS_DELAY
TRIP_CANCELLED
```

Templates must not contain hardcoded tenant-specific business logic.

---

# 8. Template Variables

Support parameterized templates.

Example:

```text
Hello {{customerName}},

Your booking {{bookingNumber}} has been confirmed.

Amount: {{amount}}
Date: {{travelDate}}
```

Variables must be validated.

Never allow arbitrary unsafe template execution.

Prevent:

* Code execution
* Template injection
* Unsafe expressions
* Unauthorized variable access

---

# 9. Localization

Minimum supported languages:

* English
* Bangla

The architecture must support additional languages without redesigning the service.

Template selection should consider:

```text
Recipient language
       ↓
Template language
       ↓
Fallback language
```

Example:

```text
bn-BD
 ↓
bn
 ↓
en
```

Do not hardcode localized text inside business logic.

---

# 10. Notification Preferences

Support recipient preferences.

Possible preferences:

```text
Email
SMS
Push
InApp
```

Preferences may include:

* Enabled/disabled
* Preferred language
* Quiet hours
* Channel priority
* Marketing opt-in
* Transactional notifications

Transactional/security notifications must not automatically be disabled by marketing preferences.

---

# 11. Notification Types

Separate notification categories.

Examples:

### Transactional

* Payment success
* Booking confirmation
* Ticket confirmation
* Password reset
* OTP
* Security alerts

### Operational

* Bus delay
* Route change
* Trip cancellation
* System status

### Marketing

* Promotions
* Offers
* Campaigns

Marketing rules must never override security/transactional requirements.

---

# 12. Notification Priority

Support priority.

Example:

```text
Critical
High
Normal
Low
```

Security and OTP notifications should receive appropriate priority.

Queue processing should respect priority where the infrastructure supports it.

---

# 13. Scheduling

Notifications must support immediate and scheduled delivery.

Examples:

```text
Send immediately
Send at specific timestamp
Send after delay
Recurring notification where appropriate
```

Use Quartz.NET for scheduled jobs where appropriate.

Do not create custom timer loops when Quartz already provides the required functionality.

---

# 14. Quartz Jobs

Possible jobs:

* Scheduled notification delivery
* Retry failed notifications
* Cleanup old notification records
* Outbox publishing
* Dead-letter processing
* Provider reconciliation
* Delivery-status synchronization

Every job must include:

* Job name
* Trigger name
* Start timestamp
* End timestamp
* Duration
* CorrelationId
* TraceId
* TenantId where applicable
* Exception
* Root cause
* Possible solution

---

# 15. Retry

Implement provider-aware retry.

Retry transient failures.

Do NOT retry:

* Invalid recipient
* Invalid template
* Authentication failure
* Invalid request
* Permanent provider rejection

Use:

* Exponential backoff
* Jitter
* Maximum retry count
* Dead-letter handling

---

# 16. Idempotency

Notification delivery must be idempotent.

Support:

```text
Idempotency-Key
```

and/or a domain-level notification idempotency key.

Example:

```text
BOOKING_CONFIRMED:BOOKING-12345
```

The same notification must not accidentally be delivered multiple times because of:

* Network retries
* Message retries
* Worker restarts
* Consumer retries
* Duplicate events

---

# 17. Outbox Pattern

Use Outbox Pattern for reliable event publishing.

Transaction:

```text
Application
    ↓
Database Transaction
    ├── Notification
    └── Outbox Event
             ↓
      Background Publisher
             ↓
          RabbitMQ
```

Outbox must support:

* Pending
* Processing
* Published
* Failed
* RetryCount
* LastAttemptAt
* Error information

Never silently lose an event.

---

# 18. Inbox / Consumer Idempotency

When consuming events, use an Inbox or equivalent deduplication mechanism.

Store:

* EventId
* EventType
* Consumer
* ReceivedAt
* ProcessedAt
* Status
* Error

Duplicate events must not cause duplicate notifications.

---

# 19. RabbitMQ Events

Potential consumed events:

```text
PaymentCompleted
PaymentFailed
BookingCreated
BookingConfirmed
BookingCancelled
BusDelayed
RouteChanged
UserRegistered
PasswordResetRequested
OtpRequested
```

Actual events must follow the existing system contracts.

Potential published events:

```text
NotificationCreated
NotificationSent
NotificationDelivered
NotificationFailed
NotificationRetryScheduled
```

Events must be:

* Versioned
* Idempotent
* Traceable
* Tenant-aware
* Correlation-aware

---

# 20. Event Contract

Every event should contain appropriate metadata:

```text
EventId
EventType
EventVersion
OccurredAt
TenantId
CompanyId
OrganizationId
CorrelationId
TraceId
Source
Payload
```

Do not expose internal database entities as event contracts.

---

# 21. REST API

Provide REST APIs where appropriate.

Examples:

```text
POST   /api/v1/notifications
GET    /api/v1/notifications/{id}
GET    /api/v1/notifications
POST   /api/v1/notifications/{id}/retry
POST   /api/v1/notifications/{id}/cancel
```

Template APIs:

```text
POST   /api/v1/templates
GET    /api/v1/templates/{id}
GET    /api/v1/templates
PUT    /api/v1/templates/{id}
DELETE /api/v1/templates/{id}
```

Preference APIs:

```text
GET /api/v1/preferences/{recipientId}
PUT /api/v1/preferences/{recipientId}
```

Actual endpoint naming must follow existing project conventions.

---

# 22. gRPC

Provide internal gRPC APIs where appropriate.

Possible operations:

```text
SendNotification
ScheduleNotification
GetNotificationStatus
CancelNotification
```

Use versioned protobuf contracts.

Do not expose persistence models.

Support:

* Authentication
* Authorization
* Deadline
* Cancellation
* CorrelationId
* TraceId
* Error mapping

---

# 23. Communication

The service must support the platform communication abstraction:

* HTTP
* gRPC
* RabbitMQ
* YARP
* Ocelot

Provider selection must be configuration-driven where applicable.

Never scatter direct transport implementations through domain/application code.

---

# 24. API Gateway

Support routing through:

* YARP
* Ocelot

The gateway handles:

* Routing
* Authentication integration
* Authorization integration
* Rate limiting
* Correlation propagation
* Distributed tracing
* Resilience

Business logic remains inside Notification Service.

---

# 25. Result Pattern

All APIs must use the centralized Result Pattern.

Example:

```json
{
  "success": false,
  "message": "Notification could not be delivered.",
  "errors": [
    {
      "code": "INVALID_RECIPIENT",
      "field": "recipientAddress",
      "message": "The recipient address is invalid."
    },
    {
      "code": "PROVIDER_UNAVAILABLE",
      "field": null,
      "message": "The notification provider is temporarily unavailable."
    }
  ],
  "traceId": "..."
}
```

Return all relevant errors where possible.

Never expose stack traces.

---

# 26. Centralized Error Handling

Use centralized exception handling.

Handle:

* Validation
* Domain exceptions
* Provider failures
* Database failures
* Message broker failures
* Timeout
* Network failures
* Concurrency
* Unexpected exceptions

Return graceful, localized messages.

---

# 27. Runtime Error Logs

Write runtime errors to:

```text
logs/runtime-error-logs/
```

Daily:

```text
runtime-error-dd-MM-yyyy.txt
```

Include:

* Timestamp
* Service name
* Environment
* Endpoint
* HTTP method
* Background service
* Quartz job
* Class
* Method
* File
* File location
* Line number
* Exception type
* Exact exception message
* Inner exception
* Root cause
* Possible solution
* TenantId
* CompanyId
* OrganizationId
* CorrelationId
* TraceId
* Provider name

Example:

```text
Timestamp:
Service:
Endpoint:
Method:
File:
Line:
Exception:
ExactMessage:
RootCause:
PossibleSolution:
CorrelationId:
TraceId:
TenantId:
CompanyId:
OrganizationId:
```

Never log secrets.

---

# 28. Build Error Logs

Write actual build errors to:

```text
logs/build-error-logs/
```

Daily:

```text
build-error-dd-MM-yyyy.txt
```

Include:

* Project
* Command
* Error code
* Exact error
* File
* Line
* Column
* Root cause
* Possible solution
* Timestamp

Never fabricate build errors.

---

# 29. Query Logs

Write query diagnostics to:

```text
logs/query-logs/
```

Daily:

```text
query-dd-MM-yyyy.txt
```

Where technically available include:

* Service
* Endpoint
* Handler
* Repository
* Method
* File
* Line
* Database provider
* Database server
* Generated query
* Safe parameters
* Start time
* End time
* Total execution time
* Rows affected
* Exception
* Root cause
* Optimization suggestion

Never log passwords, tokens, connection strings, or sensitive notification payloads.

---

# 30. Database Abstraction

Primary database:

```text
PostgreSQL
```

Support configuration-driven providers where technically feasible:

* PostgreSQL
* SQL Server
* MySQL
* Oracle
* SQLite
* MS Access where applicable
* MongoDB where appropriate

Example:

```text
Database:Provider=PostgreSQL
```

Use a database provider factory/abstraction.

Business logic must not depend on database-specific implementation.

---

# 31. Repository / CQRS

Use the existing project architecture.

Where established, use:

* Clean Architecture
* CQRS
* MediatR
* FluentValidation
* Repository
* Unit of Work

Do not introduce unnecessary abstractions.

Do not create repositories that merely wrap every EF method without adding value.

---

# 32. Audit Logging

Audit:

* Notification creation
* Template creation/update
* Template deletion
* Preference changes
* Manual retry
* Manual cancellation
* Provider changes
* Administrative configuration changes

Include:

* TenantId
* CompanyId
* OrganizationId
* UserId
* Action
* ResourceId
* Timestamp
* IP
* User agent
* CorrelationId
* TraceId

---

# 33. Security

Protect notification data.

Never expose:

* Provider credentials
* API keys
* SMTP passwords
* Access tokens
* Internal stack traces
* Sensitive message content unnecessarily

Apply:

* Authentication
* Authorization
* Tenant isolation
* Rate limiting
* Input validation
* Output validation
* Secret management

---

# 34. Rate Limiting

Protect:

* Send notification
* Bulk send
* Retry
* Template APIs
* Preference APIs

Support limits by:

* IP
* User
* Tenant
* Company
* Organization
* Endpoint

Bulk notification operations must have stricter controls.

---

# 35. Resilience

Use the platform resilience abstraction.

Support:

* Timeout
* Retry
* Circuit breaker
* Exponential backoff
* Jitter

Provider failures must not crash the entire Notification Service.

If a dependency is unavailable, return a graceful message and write detailed runtime diagnostics.

---

# 36. Caching

Use Redis where useful.

Possible cache:

* Templates
* Provider configuration
* Notification preferences
* Supported languages

Cache keys must be tenant-aware.

Never use Redis as the authoritative notification database.

---

# 37. Health Checks

Provide:

### Liveness

Process is running.

### Readiness

Required dependencies are available.

Check where applicable:

* Database
* RabbitMQ
* Redis
* Notification providers

Do not expose credentials or secrets through health endpoints.

---

# 38. Observability

Use the platform observability stack:

* Serilog
* OpenTelemetry
* Metrics
* Distributed tracing
* Health checks

Support:

* Seq
* Jaeger
* Prometheus
* Grafana
* Kibana
* Graylog where configured

Useful metrics:

* Notifications requested
* Notifications sent
* Notifications delivered
* Notifications failed
* Retry count
* Queue depth
* Provider latency
* Provider error rate
* Template rendering latency
* Database latency
* API latency

---

# 39. Docker

Provide production-ready Docker support.

Include:

* Dockerfile
* Health checks
* Environment configuration
* Secret configuration
* Non-root execution where appropriate

Never hardcode credentials.

---

# 40. Testing

Maintain:

```text
tests/
├── unit/
├── integration/
└── load-test/
```

Unit tests must cover:

* Notification creation
* Template validation
* Template rendering
* Localization
* Preferences
* Provider selection
* Retry rules
* Idempotency
* Result Pattern
* Tenant isolation

Integration tests must cover:

* REST APIs
* gRPC
* Database
* RabbitMQ
* Outbox
* Inbox/deduplication
* Redis where used
* Provider adapters
* Health checks
* Authorization

---

# 41. Performance Testing

Mandatory:

```text
tests/load-test/
```

Include:

## NBomber

* Notification creation
* Query APIs
* Template APIs
* Concurrent delivery workflows
* Stress testing

## k6

* REST API load
* Burst traffic
* Concurrent sends
* Stress testing

## Apache JMeter

* API performance
* Concurrent notification requests
* Template operations
* Provider workflow performance

Create:

```text
docs/programmers-guide/notification-performance-testing.md
```

Document exact commands and how to interpret results.

Never run destructive load tests against production.

---

# 42. Developer Guide

Maintain:

```text
docs/programmers-guide/
```

Include:

* Notification architecture
* Folder structure
* Entity creation
* CRUD
* CQRS
* Validation
* Repository
* Database migration
* Database provider
* Provider factory
* Email provider
* SMS provider
* Push provider
* Template creation
* Localization
* Notification preferences
* Quartz
* Cron expressions
* Background workers
* RabbitMQ
* Outbox
* Inbox
* HTTP
* gRPC
* YARP
* Ocelot
* Redis
* Error handling
* Runtime logging
* Build logging
* Query logging
* Audit
* SaaS
* Tenant isolation
* Idempotency
* CorrelationId
* Retry
* Circuit breaker
* Rate limiting
* OpenTelemetry
* Testing
* Load testing
* Docker
* CI/CD
* Troubleshooting
* Best practices

---

# 43. Database Guide

Create:

```text
docs/programmers-guide/notification-database.md
```

Document verified root-level commands for:

* Add migration
* Update database
* Remove migration
* List migrations
* Rollback/revert

Never invent commands.

Inspect the actual repository before writing commands.

---

# 44. Git Rules

After every logical milestone:

1. Inspect changed files.
2. Build affected projects.
3. Run applicable tests.
4. Update documentation.
5. Review the implementation.
6. Produce a professional Git commit message.
7. Continue automatically.

Never:

* Delete `.git`
* Reinitialize Git
* Rewrite history
* Force push
* Modify unrelated services

---

# 45. Completion Criteria

Notification Service is complete only when the implemented architecture supports:

* Email
* SMS
* Push
* In-app notifications
* Templates
* Template variables
* Localization
* Notification preferences
* Scheduling
* Quartz
* Retry
* Idempotency
* Outbox
* Inbox/deduplication
* RabbitMQ
* HTTP
* gRPC
* YARP
* Ocelot
* Database abstraction
* Provider abstraction
* PostgreSQL
* Centralized Result Pattern
* Centralized error handling
* Runtime error logging
* Build error logging
* Query logging
* Audit logging
* SaaS isolation
* Tenant/Company/Organization context
* Authentication
* Authorization
* Rate limiting
* Retry
* Circuit breaker
* Redis where appropriate
* OpenTelemetry
* Health checks
* Docker
* CI/CD
* Unit tests
* Integration tests
* NBomber
* k6
* JMeter
* Developer documentation

Do not claim a feature is implemented unless it actually exists and has been verified.

Do not create fake provider implementations.

If an external provider requires credentials or infrastructure that is unavailable, implement the real abstraction and document the exact configuration required.

Do not leave unexplained TODO/FIXME/HACK/stub implementations.

---

# 46. Final Verification

Before reporting completion:

1. Build the affected projects.
2. Run unit tests.
3. Run integration tests where infrastructure is available.
4. Verify database migrations.
5. Verify health checks.
6. Verify REST APIs.
7. Verify gRPC.
8. Verify RabbitMQ integration.
9. Verify outbox/inbox.
10. Verify provider abstraction.
11. Verify localization.
12. Verify tenant isolation.
13. Verify idempotency.
14. Verify retry/circuit breaker.
15. Verify logging.
16. Verify OpenTelemetry.
17. Verify Docker.
18. Verify load-test structure.
19. Update documentation.
20. Review Git changes.

Only then declare the Notification Service complete.
