# MASTER-RULE.md

# Enterprise AI Development Master Rules

## 1. Purpose

This document defines the mandatory engineering rules for every service, application, worker, API, frontend, infrastructure component, and shared package in this Enterprise Transport Platform.

These rules are designed to ensure that every component is:

* Production-ready
* Enterprise-grade
* SaaS-ready
* Secure
* Observable
* Testable
* Maintainable
* Reusable
* Independently deployable
* Docker-ready
* CI/CD-ready
* Suitable for commercial products

These rules apply to ALL services.

Service-specific requirements may extend these rules but must not silently contradict them.

---

# 2. Mandatory Reading Order

Before modifying any service, the AI/developer MUST read:

```text
1. MASTER-RULE.md
2. AI_RULES.md
3. communication.md
4. observability.md
5. testing-and-performance.md
6. Service-specific requirement file
7. Existing source code
8. Existing documentation
```

Then inspect the actual repository architecture before making decisions.

Never assume that a pattern exists merely because it is described in documentation.

Verify the implementation.

---

# 3. Golden Rule

## Read → Understand → Plan → Implement → Verify → Document → Commit → Continue

Never:

```text
Guess → Code → Hope
```

The AI must understand the existing architecture before modifying it.

---

# 4. Existing Architecture Has Priority

The existing production architecture must be respected.

Do NOT:

* Rewrite the entire solution
* Replace working architecture unnecessarily
* Rename everything
* Move unrelated projects
* Replace frameworks without justification
* Remove working features
* Introduce duplicate infrastructure
* Introduce competing patterns without reason

Reuse existing:

* Libraries
* Abstractions
* Middleware
* Logging
* Authentication
* Authorization
* Result Pattern
* Database abstractions
* Communication abstractions
* Shared contracts
* Configuration conventions

when appropriate.

---

# 5. Service Boundary

Every service must have a clearly defined responsibility.

A service must:

* Own its business domain
* Own its database/schema where applicable
* Expose contracts rather than internal entities
* Communicate through approved interfaces
* Never directly access another service's database

Forbidden:

```text
Service A
   ↓
Service B Database
```

Required:

```text
Service A
   ↓
HTTP / gRPC / Event
   ↓
Service B
   ↓
Service B Database
```

---

# 6. No Cross-Service Database Access

This is NON-NEGOTIABLE.

A service must never:

* Query another service's tables
* Modify another service's database
* Share EF entities between service boundaries
* Use another service's connection string
* Depend on another service's internal repository

Use:

* HTTP
* gRPC
* RabbitMQ
* Events
* Approved gateway communication

instead.

---

# 7. Clean Architecture

Where applicable use:

```text
Domain
Application
Infrastructure
API
```

Responsibilities:

### Domain

Contains:

* Entities
* Value Objects
* Aggregates
* Domain Events
* Business Rules

Must not depend on:

* EF Core
* HTTP
* RabbitMQ
* Redis
* Controllers
* External providers

### Application

Contains:

* Commands
* Queries
* Handlers
* DTOs
* Validators
* Interfaces
* Application services

### Infrastructure

Contains:

* EF Core
* Database
* Repositories
* Redis
* RabbitMQ
* External providers
* File systems
* Infrastructure implementations

### API

Contains:

* Controllers/endpoints
* Middleware
* Authentication configuration
* Authorization
* API configuration
* Dependency injection composition

---

# 8. SOLID

Follow SOLID.

Especially:

* Single Responsibility
* Open/Closed
* Liskov Substitution
* Interface Segregation
* Dependency Inversion

Do not create abstractions merely for the sake of abstraction.

Abstractions must solve an actual architectural problem.

---

# 9. DRY

Do not duplicate:

* Business rules
* Validation
* Logging
* Error handling
* Provider selection
* Communication code
* Authentication logic
* Authorization logic

Reuse existing shared infrastructure.

---

# 10. KISS

Enterprise does NOT mean unnecessarily complicated.

Prefer:

```text
Simple + Correct + Extensible
```

over:

```text
Complex + Clever + Fragile
```

Do not introduce:

* Generic frameworks inside the framework
* Unnecessary factories
* Unnecessary abstractions
* Excessive inheritance
* Over-engineered patterns

unless they provide real value.

---

# 11. DDD

Use Domain-Driven Design where appropriate.

Identify:

* Aggregates
* Entities
* Value Objects
* Domain Services
* Domain Events
* Invariants

Do not force DDD patterns into trivial CRUD where they add no value.

---

# 12. CQRS

Use CQRS where the architecture requires it.

Commands change state.

Queries retrieve state.

Do not mix business mutations into query handlers.

Do not perform hidden writes inside GET operations.

---

# 13. MediatR

If MediatR is already part of the architecture:

Use it consistently for:

* Commands
* Queries
* Notifications where appropriate

Do not bypass the established pipeline without a reason.

---

# 14. Validation

Use centralized validation.

Prefer:

* FluentValidation
* Domain validation
* Application validation

Validation must return ALL relevant validation errors where possible.

Do not stop at the first trivial error if multiple independent errors can be detected.

---

# 15. Result Pattern

All APIs must use the centralized Result Pattern.

Expected structure:

```json
{
  "success": false,
  "message": "The request could not be completed.",
  "errors": [
    {
      "code": "VALIDATION_ERROR",
      "field": "email",
      "message": "Email is required."
    },
    {
      "code": "INVALID_PASSWORD",
      "field": "password",
      "message": "Password does not meet security requirements."
    }
  ],
  "traceId": "..."
}
```

The frontend must be able to understand all relevant errors from a single API response.

Never expose:

* Stack traces
* Connection strings
* SQL credentials
* Internal infrastructure details
* Secret values

---

# 16. Centralized Error Handling

Never implement scattered try/catch blocks for generic API error handling.

Use centralized middleware/pipeline.

Handle:

* Validation exceptions
* Domain exceptions
* Database exceptions
* Concurrency exceptions
* Authentication exceptions
* Authorization exceptions
* Network exceptions
* Timeout exceptions
* Provider exceptions
* Unexpected exceptions

Every error should produce:

```text
Error Code
User-friendly Message
HTTP Status
TraceId
CorrelationId where applicable
```

---

# 17. Graceful Dependency Failure

If a dependency is unavailable:

Examples:

* Database unavailable
* RabbitMQ unavailable
* Redis unavailable
* Notification provider unavailable
* Payment provider unavailable
* External API unavailable

The service must NOT crash unnecessarily.

Return a graceful error.

Example:

```text
The requested operation is temporarily unavailable because the required
dependency could not be reached. Please try again later.
```

The detailed technical reason belongs in logs.

---

# 18. Runtime Error Logging

Every service must support structured runtime error logging.

Directory:

```text
logs/runtime-error-logs/
```

Daily file:

```text
runtime-error-dd-MM-yyyy.txt
```

Every meaningful exception should include:

```text
Timestamp
ServiceName
Environment
Endpoint
HttpMethod
BackgroundService
QuartzJob
ClassName
MethodName
FileName
FileLocation
LineNumber
ExceptionType
ExactExceptionMessage
InnerException
RootCause
PossibleSolution
TenantId
CompanyId
OrganizationId
UserId where safe
CorrelationId
TraceId
Provider
```

The goal is:

> A developer should be able to locate and understand the problem without hunting through the entire codebase.

---

# 19. Build Error Logging

Build/compile failures must be documented when encountered.

Directory:

```text
logs/build-error-logs/
```

Daily:

```text
build-error-dd-MM-yyyy.txt
```

Include:

```text
Timestamp
Project
Command
ErrorCode
ExactMessage
File
Line
Column
RootCause
PossibleSolution
```

Never fabricate build errors.

Only record actual errors encountered.

---

# 20. Query Logging

Query diagnostics must be available.

Directory:

```text
logs/query-logs/
```

Daily:

```text
query-dd-MM-yyyy.txt
```

Where technically supported record:

```text
Timestamp
ServiceName
Endpoint
Handler
Repository
ClassName
MethodName
FileName
FileLocation
LineNumber
DatabaseProvider
DatabaseServer
GeneratedQuery
SafeParameters
StartedAt
EndedAt
ExecutionTime
RowsAffected
Exception
RootCause
OptimizationSuggestion
```

Supported database identifiers may include:

```text
PostgreSQL
SQL Server
MySQL
Oracle
SQLite
MS Access
MongoDB
```

Never log:

* Passwords
* Tokens
* Connection strings
* Secrets
* Sensitive personal data unnecessarily

---

# 21. Database Abstraction

Primary database provider:

```text
PostgreSQL
```

The architecture should support provider abstraction where technically feasible.

Potential providers:

```text
PostgreSQL
SQL Server
MySQL
Oracle
SQLite
MS Access
MongoDB
```

Provider selection should be configuration-driven.

Example:

```text
Database:Provider=PostgreSQL
```

Business logic must not depend on a specific database provider.

---

# 22. Database Provider Factory

Where multiple database providers genuinely need to be supported, use an abstraction/factory.

Example:

```text
IDatabaseProviderFactory
```

Conceptually:

```text
DatabaseProviderFactory
        │
 ┌──────┼──────────────┐
 │      │              │
Postgres MySQL       SQLServer
 │
Oracle
 │
SQLite
```

MongoDB requires an appropriate document-oriented implementation.

Do not pretend MongoDB is relational.

---

# 23. Migrations

Every service with a relational database must maintain migrations.

Developer documentation MUST contain exact commands for:

```text
Add migration
Update database
Remove migration
List migrations
Rollback/revert
```

Commands must be verified against the actual repository.

Never invent project names or paths.

---

# 24. SaaS Architecture

All applicable services must be SaaS-ready.

Support:

```text
Tenant
 └── Company
      └── Organization
           └── Users
           └── Data
```

Where applicable every business entity must carry:

```text
TenantId
CompanyId
OrganizationId
```

The exact hierarchy may vary by service.

---

# 25. Tenant Isolation

Tenant isolation is mandatory.

Every query must enforce tenant scope.

Never trust a client-provided TenantId blindly.

Resolve tenant context from trusted authentication/authorization context where possible.

Forbidden:

```text
SELECT * FROM Orders
```

without tenant filtering when the table is tenant-scoped.

---

# 26. Company and Organization Isolation

Where applicable:

```text
TenantId
CompanyId
OrganizationId
```

must be validated together.

A user from:

```text
Tenant A
Company A
Organization A
```

must never access:

```text
Tenant B
Company B
Organization B
```

---

# 27. Authentication

Authentication must be centralized.

Use the platform authentication architecture.

Where Identity Framework / Identity Server are used, follow the existing implementation rather than creating local authentication systems.

---

# 28. Authentication Requirements

The reusable authentication package should support:

* Login
* Logout
* Access token
* Refresh token
* OTP
* Two-factor authentication
* Forgot password
* Reset password
* Security questions where required
* Password history
* Account lockout
* Session management
* Device/session tracking where applicable
* Email/phone verification

---

# 29. Password History

The last 3 passwords MUST NOT be reusable as a new password.

Password history validation must occur server-side.

Never store plaintext passwords.

Use the established identity framework's secure password hashing.

---

# 30. Security Questions

If security questions are enabled:

* Store securely
* Never store plaintext answers
* Hash answers appropriately
* Normalize answers consistently
* Rate-limit verification attempts
* Lock/slow repeated failures
* Audit security-question changes

Do not expose security answers through APIs.

---

# 31. Two-Factor Authentication

Support:

* OTP
* Trusted device/session handling where applicable
* Recovery flow
* Rate limiting
* Expiration
* Attempt limits

OTP must:

* Expire
* Be single-use
* Have attempt limits
* Be rate-limited
* Never appear in logs

---

# 32. Authorization

Use centralized authorization.

Support:

* Roles
* Permissions
* Policies
* Claims
* Tenant scope
* Company scope
* Organization scope

Never rely only on frontend authorization.

---

# 33. Permission Management

Permissions should be reusable across applications.

Examples:

```text
route.read
route.create
route.update
route.delete

payment.read
payment.create

notification.read
notification.send
```

Do not hardcode authorization decisions in controllers.

---

# 34. Module Management

The platform should support module-based authorization where applicable.

Examples:

```text
Authentication
Users
Routes
Buses
Payments
Notifications
Bookings
Reports
Administration
```

Users/roles can be granted permissions within modules.

---

# 35. Communication Architecture

Services must support the platform communication abstraction.

Supported mechanisms:

```text
HTTP
gRPC
RabbitMQ
YARP
Ocelot
```

Use the correct mechanism for the communication requirement.

---

# 36. Communication Selection

### HTTP

Use for:

* REST
* External APIs
* Simple request/response

### gRPC

Use for:

* Internal synchronous service-to-service calls
* High-performance internal communication

### RabbitMQ

Use for:

* Asynchronous events
* Decoupling
* Background processing
* Event-driven workflows

### YARP / Ocelot

Use for:

* API Gateway
* Routing
* Gateway policies
* External API aggregation where appropriate

Do NOT use an API gateway as a replacement for internal service boundaries.

---

# 37. Communication Factory

Communication implementations must be abstracted where provider switching is genuinely required.

Example:

```text
ICommunicationProviderFactory
```

Possible providers:

```text
HTTP
gRPC
RabbitMQ
```

Configuration may select:

```text
Communication:Provider=Grpc
```

Business logic must not depend directly on transport implementation.

---

# 38. Idempotency-Key

All appropriate state-changing APIs must support:

```text
Idempotency-Key
```

Especially:

* Payments
* Orders
* Bookings
* Notifications
* Financial operations
* External provider operations

Same key + same operation:

```text
Same logical result
```

Same key + different payload:

```text
Reject
```

Do not blindly add idempotency to operations where it provides no value.

---

# 39. CorrelationId

Every request should support:

```text
CorrelationId
```

If the client does not provide one, generate it.

Propagate it across:

```text
Frontend
 ↓
Gateway
 ↓
Service A
 ↓
Service B
 ↓
RabbitMQ
 ↓
Background Worker
```

---

# 40. TraceId

Use distributed tracing through OpenTelemetry.

CorrelationId and TraceId are different concepts.

Do not replace one with the other.

---

# 41. IP Trace

Where appropriate and legally/operationally justified, capture:

* Client IP
* Forwarded IP chain
* User agent

Never blindly trust arbitrary forwarding headers.

Use trusted proxy configuration.

---

# 42. Rate Limiting

Implement centralized and service-level rate limiting.

Support appropriate dimensions:

```text
IP
User
Tenant
Company
Organization
Endpoint
API key
```

Sensitive endpoints require stricter limits.

Especially:

* Login
* OTP
* Password reset
* Security questions
* Payment
* Bulk operations

---

# 43. Resilience

Use Polly or the established .NET resilience stack.

Support:

* Timeout
* Retry
* Exponential backoff
* Jitter
* Circuit breaker

Do not retry permanent failures.

Do not create retry storms.

---

# 44. Circuit Breaker

Circuit breakers must protect the system from failing dependencies.

Concept:

```text
Closed
  ↓
Failure threshold
  ↓
Open
  ↓
Recovery timeout
  ↓
Half Open
  ↓
Success → Closed
Failure → Open
```

Configure thresholds based on actual dependency behavior.

---

# 45. Retry

Retry only transient failures.

Examples:

```text
Timeout
Temporary network failure
Temporary broker failure
Temporary provider unavailable
```

Do not retry:

```text
400
401
403
Validation error
Business rule failure
Invalid request
```

unless the specific protocol explicitly requires otherwise.

---

# 46. Outbox Pattern

For reliable event publication:

```text
Business Transaction
        │
        ├── Domain Change
        └── Outbox Event
                ↓
        Background Publisher
                ↓
             Broker
```

Never publish an event and database transaction independently when consistency is required.

---

# 47. Inbox Pattern

Consumers should support deduplication.

Store:

```text
EventId
EventType
Consumer
ReceivedAt
ProcessedAt
Status
```

Duplicate events must be safe.

---

# 48. Event Design

Events must be:

* Versioned
* Immutable
* Idempotent
* Tenant-aware
* Correlation-aware
* Traceable

Do not publish internal EF entities.

---

# 49. Redis

Redis may be used for:

* Caching
* Distributed locks where appropriate
* Rate limiting
* Temporary state
* Session-like data where appropriate

Never treat Redis as the permanent source of truth unless explicitly designed as such.

Tenant-scoped cache keys are mandatory.

---

# 50. API Design

REST APIs must:

* Use consistent naming
* Use versioning
* Validate input
* Return Result Pattern
* Support pagination
* Support filtering
* Support sorting
* Support cancellation
* Support correlation
* Return appropriate HTTP status codes

Never return:

```text
200 OK
```

for every possible failure.

---

# 51. Pagination

Collection endpoints must be paginated.

Support:

```text
page
pageSize
sortBy
sortDirection
```

Enforce maximum page size.

Never allow unlimited database result sets.

---

# 52. Search and Filtering

Search must execute at the database layer.

Never:

```text
Load everything
↓
Filter in memory
```

unless the dataset is intentionally tiny and the design explicitly requires it.

Use proper indexes.

---

# 53. Optimistic Concurrency

State-changing entities must use optimistic concurrency where appropriate.

If stale data is submitted:

Return a controlled concurrency error.

Never silently overwrite another user's changes.

---

# 54. Soft Delete

Where business requirements require historical preservation:

Use soft delete.

Potential fields:

```text
IsDeleted
DeletedAt
DeletedBy
```

Default queries must exclude deleted records unless explicitly requested.

Never physically delete regulated/audited data without a documented reason.

---

# 55. Audit Logging

Audit important business/security actions.

Include:

```text
TenantId
CompanyId
OrganizationId
UserId
Action
Resource
ResourceId
Timestamp
IP
UserAgent
CorrelationId
TraceId
Before
After
```

Do not store unnecessary sensitive data.

---

# 56. Localization

Minimum:

```text
English
Bangla
```

Architecture must allow:

```text
Arabic
Hindi
Spanish
French
Chinese
etc.
```

without rewriting business logic.

Use resource-based localization.

Never hardcode user-facing messages throughout the application.

Error codes must remain stable across languages.

---

# 57. Logging

Use structured logging.

Preferred stack:

```text
Serilog
   ↓
OpenTelemetry
   ↓
Seq / Elasticsearch / Loki / Graylog
```

depending on the deployment architecture.

Never log:

* Passwords
* OTP
* Tokens
* API secrets
* Connection strings
* Private keys

---

# 58. Observability

All services should support:

```text
OpenTelemetry
Prometheus
Grafana
Jaeger
Seq
Kibana
Graylog
```

where configured by the platform.

Observe:

* Requests
* Errors
* Dependencies
* Database
* Messaging
* Background jobs
* Metrics
* Distributed traces

---

# 59. Health Checks

Every service must provide:

### Liveness

Application process is alive.

### Readiness

Required dependencies are available.

Potential dependencies:

* PostgreSQL
* RabbitMQ
* Redis
* External providers

Do not expose secrets through health endpoints.

---

# 60. API Documentation

Use OpenAPI.

Where the existing architecture uses Scalar:

Use:

```text
OpenAPI + Scalar
```

Keep API documentation synchronized with implementation.

---

# 61. Testing Pyramid

Every service should contain:

```text
Unit Tests
Integration Tests
API Tests
Load Tests
Stress Tests
Performance Tests
```

Do not rely exclusively on integration tests.

---

# 62. Mandatory Load-Test Structure

Every service must contain:

```text
tests/
└── load-test/
    ├── nbomber/
    ├── k6/
    └── jmeter/
```

Where the repository's existing structure differs, preserve conventions while ensuring equivalent separation.

---

# 63. NBomber

Use NBomber for:

* .NET-native load testing
* Service-level performance
* Concurrent workloads
* Stress testing

---

# 64. k6

Use k6 for:

* HTTP load
* API stress
* Spike testing
* Soak testing
* Threshold validation

---

# 65. JMeter

Use Apache JMeter for:

* API performance
* Concurrent requests
* Scenario-based testing
* Enterprise performance testing

---

# 66. Performance Documentation

Every service must document:

```text
How to run NBomber
How to run k6
How to run JMeter
Where results are generated
How to interpret results
Expected thresholds
How to compare runs
```

Never run destructive stress tests against production.

---

# 67. Unit Tests

Unit tests must test business behavior.

Focus on:

* Domain rules
* Validation
* Commands
* Queries
* Policies
* Calculations
* Result Pattern
* Error conditions

Avoid writing tests that merely verify framework behavior.

---

# 68. Integration Tests

Integration tests should verify real integration boundaries:

* Database
* Message broker
* Redis
* HTTP
* gRPC
* Authentication
* Authorization
* Outbox
* Inbox

Use containers/Testcontainers where appropriate.

---

# 69. Security Testing

Test:

* Authentication
* Authorization
* Tenant isolation
* Permission boundaries
* Rate limiting
* Input validation
* Injection resistance
* Secret exposure
* Token handling
* Password policy
* OTP protection

---

# 70. Docker

Every independently deployable service must be Docker-ready.

Requirements:

* Multi-stage build
* Minimal runtime image
* Non-root user where possible
* Health check
* Environment configuration
* No hardcoded secrets

---

# 71. Configuration

Configuration must be externalized.

Use:

* appsettings
* Environment variables
* Secret managers
* Deployment configuration

Never hardcode:

* Passwords
* API keys
* Tokens
* Connection strings
* Provider credentials

---

# 72. CI/CD

Pipeline should support:

```text
Restore
 ↓
Build
 ↓
Unit Tests
 ↓
Integration Tests
 ↓
Static Analysis
 ↓
Security/Dependency Checks
 ↓
Docker Build
 ↓
Artifact
 ↓
Deployment
```

The exact pipeline follows repository conventions.

---

# 73. Code Quality

Before completion:

* Remove dead code
* Remove unused imports
* Remove unnecessary dependencies
* Remove duplicate logic
* Fix warnings where practical
* Avoid suppressing warnings without reason
* Avoid TODO/FIXME/HACK
* Avoid commented-out production code

---

# 74. No Fake Implementations

NEVER create:

```text
TODO
throw new NotImplementedException()
return null
return true
return false
fake provider
mock production service
placeholder repository
```

unless the item is explicitly part of a test.

Production code must be real.

---

# 75. No Silent Failures

Never swallow exceptions.

Forbidden:

```csharp
catch
{
}
```

If an exception is intentionally handled:

* Log it
* Explain why
* Return appropriate result
* Preserve trace/correlation context

---

# 76. Cancellation

Long-running and I/O operations should support:

```text
CancellationToken
```

Propagate cancellation through:

```text
API
 ↓
Application
 ↓
Infrastructure
 ↓
Database / HTTP / gRPC / Messaging
```

Do not ignore cancellation unnecessarily.

---

# 77. Async Programming

Use async/await for I/O.

Avoid:

```text
.Result
.Wait()
Thread.Sleep()
```

inside asynchronous server workflows.

Use asynchronous APIs throughout.

---

# 78. Database Performance

Avoid:

* N+1 queries
* Unbounded queries
* Excessive Includes
* Loading unnecessary columns
* In-memory filtering
* Unindexed search
* Long-running transactions

Use:

* Projection
* Pagination
* Appropriate indexes
* Query optimization
* AsNoTracking for read-only queries where appropriate

---

# 79. Secrets

Never commit:

```text
.env
secrets
passwords
API keys
private keys
connection strings
tokens
```

Use secure configuration.

---

# 80. Dependency Management

Before adding a package:

1. Check whether an existing dependency already solves the problem.
2. Check compatibility.
3. Check licensing.
4. Check security.
5. Check maintenance status.
6. Avoid unnecessary packages.

Do not add libraries simply because they are popular.

---

# 81. API Compatibility

Do not break existing API contracts without an explicit migration strategy.

For breaking changes:

* Version the API
* Document migration
* Preserve backward compatibility where possible

---

# 82. Contract Versioning

Version:

* REST APIs
* gRPC contracts
* Events
* Messages

Never silently change the meaning of an existing event.

---

# 83. Event Compatibility

Prefer additive event changes.

Avoid breaking existing consumers.

If breaking change is unavoidable:

```text
EventV1
EventV2
```

and migrate consumers gradually.

---

# 84. File and Folder Discipline

Keep files in logical locations.

Do not create random:

```text
Utils
Helpers
Misc
Temp
NewFolder
```

without architectural justification.

Prefer meaningful names.

---

# 85. Documentation

Every service must maintain:

```text
docs/programmers-guide/
```

Documentation should explain:

* Architecture
* Folder structure
* CRUD
* Entity creation
* CQRS
* Validation
* Repository
* Database
* Migrations
* Communication
* Events
* Quartz
* Background workers
* Error handling
* Logging
* Observability
* Testing
* Performance
* Docker
* CI/CD
* Troubleshooting

Documentation must describe the ACTUAL implementation.

---

# 86. Developer Commands

Documentation must contain verified commands for:

```text
Restore
Build
Run
Test
Add migration
Update database
Run Docker
Run integration tests
Run NBomber
Run k6
Run JMeter
```

Never document unverified commands as fact.

---

# 87. Git Safety

The `.git` directory is sacred.

NEVER:

* Delete `.git`
* Run `git init` over an existing repository
* Rewrite history
* Force push
* Reset unrelated work
* Delete another developer's commits
* Remove branches without explicit permission

---

# 88. Git Commit Rules

After every logical milestone:

```text
Inspect
Build
Test
Document
Review
Commit
Continue
```

Commit messages must be professional.

Examples:

```text
feat(notification): implement notification template management

feat(route): add route topology validation

feat(payment): implement payment provider abstraction

fix(auth): enforce password history policy

test(bus): add integration tests for bus search

perf(route): optimize route search query

docs(notification): add Quartz developer guide
```

Use conventional commit style.

---

# 89. Commit Granularity

Do not make one enormous commit containing the entire service.

Prefer logical milestones.

Example:

```text
feat: establish notification domain

feat: implement notification CRUD

feat: add notification provider abstraction

feat: add RabbitMQ integration

feat: implement outbox processing

test: add notification integration tests

docs: add notification programmer guide
```

---

# 90. Milestone Verification

After every milestone:

1. Build affected projects.
2. Run relevant tests.
3. Inspect compiler warnings/errors.
4. Review changed files.
5. Update documentation.
6. Create professional commit message.
7. Continue.

Do not stop merely because one milestone succeeded.

---

# 91. Unrelated Changes

Do NOT modify unrelated services.

If a required change crosses a service boundary:

STOP and request approval ONLY when the change affects:

* Shared database
* Shared contract
* Shared infrastructure
* Authentication architecture
* Global architecture
* Breaking API contract

Minor implementation decisions should be made automatically.

---

# 92. Do Not Ask Unnecessary Questions

The AI should make reasonable engineering decisions.

Do not stop for:

* Naming trivial variables
* Choosing a normal class name
* Choosing obvious folder placement
* Minor validation decisions
* Standard HTTP status codes
* Routine implementation details
* Documentation wording

Ask only when the decision genuinely affects architecture or another service.

---

# 93. Existing Tests Are Contracts

Do not delete or weaken existing tests simply to make the build pass.

If an existing test conflicts with a legitimate requirement:

* Understand why
* Update it carefully
* Preserve intended behavior

Never manipulate tests to hide defects.

---

# 94. Existing Functionality

Never remove existing functionality unless:

* It is demonstrably broken
* The replacement preserves behavior
* The change is documented
* No unrelated functionality is affected

---

# 95. Performance Principle

Do not optimize blindly.

First:

```text
Measure
 ↓
Identify bottleneck
 ↓
Optimize
 ↓
Measure again
```

Use metrics and profiling where possible.

---

# 96. Reliability Principle

A service must assume:

```text
Network can fail
Database can fail
Broker can fail
Redis can fail
Provider can fail
Messages can duplicate
Requests can retry
Workers can restart
Containers can restart
```

Design accordingly.

---

# 97. Distributed Systems Principle

Never assume:

```text
Exactly once
Instant delivery
Perfect network
Shared transaction
```

Prefer:

```text
At-least-once delivery
Idempotency
Retries
Outbox
Inbox
Timeouts
Circuit breakers
Observability
```

---

# 98. Background Jobs

Background workers and Quartz jobs must be:

* Idempotent
* Observable
* Retry-safe
* Cancellation-aware
* Tenant-aware where applicable
* Concurrency-safe

Never assume a job runs only once.

---

# 99. Scheduled Job Documentation

Every Quartz job must document:

```text
Job name
Purpose
Trigger
Cron expression
Retry behavior
Concurrency behavior
Failure behavior
Required dependencies
How to run manually
How to troubleshoot
```

---

# 100. Production Readiness

A service is NOT production-ready merely because:

```text
dotnet build
```

passes.

Production readiness requires:

```text
Build
+
Tests
+
Security
+
Database
+
Observability
+
Resilience
+
Logging
+
Communication
+
Docker
+
CI/CD
+
Documentation
+
Performance
```

---

# 101. Completion Declaration

Never declare a service "complete" merely because the requested source files were created.

Before completion verify:

```text
Architecture
Domain
Application
Infrastructure
API
Database
Migrations
Security
Authentication
Authorization
SaaS isolation
Communication
Messaging
Outbox
Inbox
Caching
Resilience
Error handling
Logging
Observability
Testing
Performance
Docker
CI/CD
Documentation
Git
```

---

# 102. Known Limitations

If something cannot be verified because infrastructure is unavailable:

DO NOT pretend it passed.

Document:

```text
Component
Status
Why it could not be verified
Required infrastructure
Exact command to verify
Expected result
```

---

# 103. Final Report

At completion provide:

```text
Completed Features

Changed Files

Database Changes

API Endpoints

gRPC Endpoints

Events

Background Jobs

Configuration Changes

Docker Changes

Tests

Performance Tests

Documentation Updated

How to Run

How to Test

How to Observe Logs

How to Inspect OpenTelemetry

Known Limitations

Suggested Next Step

Professional Git Commit History
```

Keep the final report concise and factual.

---

# 104. Absolute Rules

These rules are NON-NEGOTIABLE:

```text
1. Never delete .git.
2. Never rewrite Git history.
3. Never force push.
4. Never modify unrelated services.
5. Never access another service's database.
6. Never expose secrets.
7. Never fake implementations.
8. Never hide exceptions.
9. Never silently swallow failures.
10. Never claim unverified tests passed.
11. Never invent documentation commands.
12. Never bypass tenant isolation.
13. Never bypass authorization.
14. Never expose stack traces to clients.
15. Never log passwords, OTPs, tokens, or secrets.
16. Never create unbounded database queries.
17. Never silently overwrite concurrent changes.
18. Never introduce unnecessary architecture.
19. Never remove working functionality without justification.
20. Never stop for trivial engineering decisions.
21. Always verify before declaring completion.
22. Always document meaningful architectural decisions.
23. Always provide professional Git commit messages.
24. Always preserve correlation and trace context.
25. Always design for failure in distributed systems.
```

---

# 105. Engineering Philosophy

Build software as if another team will maintain it for the next ten years.

Code should be:

```text
Boring where it should be boring.
Elegant where elegance provides value.
Strict where correctness matters.
Flexible where change is expected.
Observable when things fail.
Simple when complexity provides no value.
```

The objective is not to produce the maximum amount of code.

The objective is to produce the smallest amount of **correct, maintainable, secure, observable, reusable production code** that satisfies the actual requirements.

---

# END OF MASTER-RULE.md
