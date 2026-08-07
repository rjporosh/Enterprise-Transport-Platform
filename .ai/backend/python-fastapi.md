# Python + FastAPI Enterprise Engineering Rules

## 1. PURPOSE

This document defines Python + FastAPI-specific engineering rules for production-grade enterprise applications and services.

It extends:

```text
.ai/MASTER-RULE.md
.ai/AI_RULES.md
.ai/communication.md
.ai/observability.md
.ai/testing-and-performance.md
.ai/backend/python-fastapi.md
```

The common platform rules remain authoritative.

This document defines how those rules are implemented using:

```text
Python
+
FastAPI
+
Pydantic
+
SQLAlchemy/approved ORM
```

Do not duplicate or contradict the common platform rules.

---

# 2. VERSION POLICY

Always use the latest stable and supported versions available at implementation time.

Do not permanently hardcode obsolete versions.

Before implementation inspect:

```text
pyproject.toml
requirements.txt
requirements/*.txt
poetry.lock
uv.lock
Pipfile
Pipfile.lock
Dockerfile
docker-compose.*
```

Select the newest compatible stable versions of:

```text
Python
FastAPI
Pydantic
Uvicorn
ORM
Database Driver
Testing Framework
```

Do not downgrade merely to avoid compatibility work.

---

# 3. EXISTING PROJECT FIRST

Before modifying the project inspect:

```text
app/
src/
tests/
alembic/
pyproject.toml
requirements*
Dockerfile
docker-compose.*
.env*
```

Determine the existing architecture.

Do not redesign the entire project merely because another structure appears cleaner.

Reuse established conventions when they are sound.

---

# 4. RECOMMENDED ARCHITECTURE

For enterprise applications prefer a layered architecture:

```text
src/
└── app/
    ├── api/
    │   ├── routes/
    │   ├── dependencies/
    │   └── middleware/
    │
    ├── application/
    │   ├── commands/
    │   ├── queries/
    │   ├── services/
    │   └── dto/
    │
    ├── domain/
    │   ├── entities/
    │   ├── value_objects/
    │   ├── services/
    │   ├── events/
    │   └── exceptions/
    │
    ├── infrastructure/
    │   ├── database/
    │   ├── repositories/
    │   ├── messaging/
    │   ├── grpc/
    │   ├── http/
    │   ├── cache/
    │   └── observability/
    │
    ├── config/
    └── main.py
```

Adapt to the existing project.

Do not create abstractions without a practical reason.

---

# 5. PYTHON VERSION

Use the latest stable Python version compatible with:

```text
FastAPI
Pydantic
ORM
Database Drivers
Production Infrastructure
```

Verify compatibility before upgrading.

Do not blindly upgrade Python in an existing production project.

---

# 6. TYPE HINTING

Use type hints throughout application code.

Prefer:

```python
def get_user(user_id: UUID) -> UserDTO:
    ...
```

over untyped functions.

Avoid:

```python
def get_user(user_id):
    ...
```

Use:

```text
Optional typing
Generics
Protocols
Typed collections
Dataclasses
Pydantic models
```

where appropriate.

---

# 7. PYTHON CODE QUALITY

Follow:

```text
PEP 8
PEP 257
SOLID
DRY
KISS
Clean Architecture
DDD where appropriate
```

Use the project's configured:

```text
ruff
black
mypy/pyright
```

or equivalent tooling.

Do not introduce multiple overlapping formatters unnecessarily.

---

# 8. ASYNC FIRST

FastAPI supports asynchronous request handling.

Use:

```python
async def
```

for I/O-bound operations when the underlying library supports async execution.

Examples:

```text
Database
HTTP
gRPC
Redis
RabbitMQ
External APIs
```

Do not use blocking operations inside async request handlers.

---

# 9. BLOCKING CODE

Avoid:

```python
time.sleep()
requests.get()
Blocking DB drivers
CPU-heavy processing
```

inside asynchronous request handlers.

Use asynchronous equivalents where available.

For CPU-heavy work use:

```text
Worker
Task Queue
Process Pool
Dedicated Service
```

where appropriate.

---

# 10. FASTAPI APPLICATION

Keep:

```text
main.py
```

small.

Typical flow:

```text
FastAPI
 ↓
Middleware
 ↓
Router
 ↓
Dependency
 ↓
Application Service
 ↓
Domain
 ↓
Repository / Adapter
 ↓
Result
 ↓
Response
```

Do not put business logic directly into route functions.

---

# 11. ROUTERS

FastAPI routers should remain thin.

Example:

```python
@router.get("/{user_id}")
async def get_user(
    user_id: UUID,
    service: UserService = Depends(get_user_service),
):
    return await service.get_user(user_id)
```

Avoid putting:

```text
Database Queries
Complex Business Rules
RabbitMQ
gRPC
Large Validation Logic
```

directly inside route functions.

---

# 12. PYDANTIC MODELS

Use Pydantic models for:

```text
Request Validation
Response DTOs
Configuration
External API Contracts
Event Contracts
```

Do not expose ORM entities directly as public API contracts.

---

# 13. REQUEST / RESPONSE DTOs

Separate:

```text
Request DTO
Application DTO
Domain Entity
Persistence Model
Response DTO
```

when the domain complexity justifies it.

Do not expose internal database structures directly to clients.

---

# 14. VALIDATION

Every external input must be validated.

Validate:

```text
Request Body
Path Parameters
Query Parameters
Headers
Event Payloads
External Service Responses
```

Never assume incoming data is trustworthy.

---

# 15. CENTRALIZED RESULT PATTERN

Use the platform-wide Result Pattern.

Example:

```json
{
  "success": false,
  "message": "Validation failed.",
  "errors": [
    {
      "code": "REQUIRED",
      "field": "email",
      "message": "Email is required."
    },
    {
      "code": "INVALID",
      "field": "phone",
      "message": "Phone number is invalid."
    }
  ],
  "traceId": "..."
}
```

Multiple independent validation/business errors should be returned together when practical.

Do not create a different response structure for every endpoint.

---

# 16. CENTRALIZED EXCEPTION HANDLING

Implement FastAPI exception handlers centrally.

Handle common categories such as:

```text
ValidationError
AuthenticationError
AuthorizationError
NotFoundError
ConflictError
ConcurrencyError
DependencyUnavailableError
TimeoutError
RateLimitError
BusinessRuleError
```

Map technical exceptions to safe API responses.

Never expose raw infrastructure exceptions to clients.

---

# 17. GRACEFUL DEPENDENCY FAILURE

If:

```text
PostgreSQL
MySQL
SQL Server
Oracle
SQLite
MongoDB
Redis
RabbitMQ
gRPC Service
HTTP Service
External API
```

is unavailable:

```text
Detect
 ↓
Log
 ↓
Trace
 ↓
Retry if safe
 ↓
Circuit Break if necessary
 ↓
Return graceful response
```

Example:

```text
The requested service is temporarily unavailable. Please try again later.
```

Technical diagnostics belong in centralized logs.

---

# 18. ERROR RESPONSE

Do not return:

```text
Database connection string
Stack trace
Internal server path
Credentials
Secret values
Raw SQL
Infrastructure topology
```

to clients.

Return:

```text
Stable Error Code
Graceful Message
Field Errors where applicable
CorrelationId/TraceId
```

---

# 19. ERROR MESSAGE LOCALIZATION

Support centralized language selection.

At minimum:

```text
English
Bangla
```

Design the message system so additional languages can be added without rewriting business logic.

Prefer:

```text
error.code
+
translation key
```

over hardcoded language-specific messages.

---

# 20. LANGUAGE SELECTION

Language may come from:

```text
User Preference
Tenant Configuration
Accept-Language
Explicit Request Context
```

according to the platform design.

Do not let language selection alter business logic.

---

# 21. AUTHENTICATION

Authentication must use the centralized platform authentication architecture.

Support integration with:

```text
Identity Provider
JWT
OAuth2/OIDC
Identity Server
Central Auth Service
```

according to the project.

Do not create independent authentication logic in every service.

---

# 22. AUTHORIZATION

Authorization must verify:

```text
User
Tenant
Company
Organization
Role
Permission
Module
Resource
Action
```

where applicable.

Client-side permission checks are not security controls.

---

# 23. MULTI-TENANCY

Tenant context must come from trusted authenticated context.

Never trust:

```text
tenantId
companyId
organizationId
```

provided only by the client.

Every tenant-aware operation must enforce isolation.

---

# 24. REQUEST CONTEXT

Where applicable, request context should contain:

```text
UserId
TenantId
CompanyId
OrganizationId
CorrelationId
TraceId
Roles
Permissions
IP
```

Keep request-context handling centralized.

Do not pass framework-specific request objects deep into domain logic.

---

# 25. CORRELATION ID

Every request must participate in the distributed correlation model.

Support:

```text
CorrelationId
TraceId
```

Propagate them through:

```text
HTTP
gRPC
RabbitMQ
Database instrumentation
Background Jobs
Logs
```

---

# 26. MIDDLEWARE

Use FastAPI middleware for cross-cutting concerns such as:

```text
Correlation
Tracing
Security Headers
Request Logging
Timing
Tenant Context
Locale
```

Do not put heavy business logic into middleware.

---

# 27. IP TRACE

When determining the client IP behind:

```text
YARP
Ocelot
NGINX
Ingress
Load Balancer
CDN
```

configure trusted proxies correctly.

Never blindly trust arbitrary:

```text
X-Forwarded-For
X-Real-IP
```

headers.

---

# 28. RATE LIMITING

Implement server-side rate limiting where required.

Possible dimensions:

```text
IP
User
Tenant
Company
API Key
Endpoint
```

Sensitive endpoints should have stricter limits.

Examples:

```text
Login
OTP
Password Reset
Forgot Password
Public APIs
Payment Operations
```

---

# 29. IDEMPOTENCY

Support:

```text
Idempotency-Key
```

for operations with external side effects.

Especially:

```text
Payment
Booking
Order
Notification
Event Publishing
External API Operations
```

Store idempotency state in a distributed store where multiple replicas are possible.

---

# 30. HTTP COMMUNICATION

Internal service communication may use:

```text
HTTP
gRPC
RabbitMQ
```

as defined by:

```text
.ai/communication.md
```

Use abstraction interfaces.

Do not scatter provider-specific code throughout business logic.

---

# 31. COMMUNICATION FACTORY

Preferred structure:

```text
Application
 ↓
IServiceCommunication
 ↓
CommunicationFactory
 ↓
HTTP Adapter
gRPC Adapter
RabbitMQ Adapter
```

Provider selection should be configuration-driven where practical.

Example:

```text
COMMUNICATION_PROVIDER=grpc
```

Changing the provider should not require rewriting application business logic.

---

# 32. YARP / OCELOT

Services must remain compatible with API gateway infrastructure such as:

```text
YARP
Ocelot
```

Do not hardcode gateway assumptions into domain logic.

Gateway routing belongs at the infrastructure boundary.

---

# 33. HTTP CLIENT

Use a centralized HTTP client implementation.

It should support:

```text
Timeout
Retry
Circuit Breaker
CorrelationId
TraceId
Authentication
Idempotency
Logging
```

where applicable.

Do not create unmanaged HTTP clients per request.

---

# 34. RETRY POLICY

Retries must be:

```text
Bounded
Observable
Exponential Backoff
Jittered where appropriate
```

Do not retry unsafe mutations unless idempotency is guaranteed.

Do not retry:

```text
Authentication failures
Validation failures
Permanent 4xx errors
```

unless explicitly required.

---

# 35. CIRCUIT BREAKER

Use circuit breaking for unstable dependencies.

Example:

```text
FastAPI
 ↓
Service Client
 ↓
Circuit Breaker
 ↓
Internal Service
```

Avoid cascading failures.

---

# 36. TIMEOUTS

Every external operation must have a bounded timeout.

Apply appropriate timeouts to:

```text
HTTP
gRPC
Database
Redis
RabbitMQ
External APIs
```

Never allow an external call to hang indefinitely.

---

# 37. DATABASE ABSTRACTION

The platform may support:

```text
PostgreSQL
SQL Server
MySQL
Oracle
SQLite
MongoDB
MS Access where technically supported by the selected adapter
```

using an abstraction/factory strategy where required.

Example configuration:

```text
DATABASE_PROVIDER=postgres
```

Do not pretend every database supports identical capabilities.

Provider-specific features must remain inside adapters.

---

# 38. PRIMARY DATABASE

Default enterprise database:

```text
PostgreSQL
```

unless the project explicitly specifies another provider.

The application should use an abstraction layer where provider switching is a real requirement.

---

# 39. ORM

Use the existing project standard.

Common options include:

```text
SQLAlchemy
SQLModel
Tortoise ORM
MongoEngine
Motor/PyMongo
```

Do not introduce multiple ORMs unnecessarily.

For enterprise relational applications, SQLAlchemy is generally preferred when it matches the existing architecture.

---

# 40. REPOSITORY PATTERN

Use repositories when they provide meaningful separation between:

```text
Application
Domain
Persistence
```

Avoid creating repository wrappers that only duplicate ORM methods without adding architectural value.

---

# 41. UNIT OF WORK

Use Unit of Work when the project requires coordinated transactional operations.

Typical flow:

```text
Application Service
 ↓
Unit of Work
 ↓
Repositories
 ↓
Commit / Rollback
```

Do not manually manage transactions inconsistently across endpoints.

---

# 42. TRANSACTIONS

Transactions must be explicit for operations requiring atomicity.

Example:

```text
Business Change
+
Audit Record
+
Outbox Event
```

should use a coordinated transaction where the database supports it.

---

# 43. OPTIMISTIC CONCURRENCY

Where concurrent updates are possible, use an appropriate concurrency strategy.

Possible mechanisms:

```text
Version Column
Timestamp
ETag
Row Version
Database-supported concurrency control
```

Do not silently overwrite another user's changes.

Return a consistent conflict error.

---

# 44. SOFT DELETE

Use soft delete for business entities where required.

Typical fields:

```text
IsDeleted
DeletedAt
DeletedBy
```

Repositories must consistently exclude deleted records unless explicitly querying deleted data.

---

# 45. AUDIT LOGGING

Critical operations must produce audit records.

Include:

```text
User
Tenant
Company
Organization
Action
Resource
ResourceId
Timestamp
CorrelationId
Result
IP
```

Never log:

```text
Password
OTP
JWT
Refresh Token
API Key
Private Key
Payment Credentials
```

---

# 46. QUERY LOGGING

Database instrumentation should capture where practical:

```text
Endpoint
Method
File
Line
Database Provider
Generated Query
Started Time
Ending Time
Total Execution Time
CorrelationId
TraceId
```

Sensitive parameters must be masked.

Do not log secrets.

---

# 47. DATABASE PERFORMANCE

Monitor:

```text
Query Duration
Connection Pool
Slow Queries
N+1 Queries
Lock Contention
Deadlocks
Index Usage
```

Do not optimize blindly.

Use actual measurements.

---

# 48. PAGINATION

Large endpoints must support pagination.

Prefer:

```text
page
pageSize
```

or cursor-based pagination where appropriate.

Return consistent metadata:

```json
{
  "page": 1,
  "pageSize": 25,
  "total": 250,
  "totalPages": 10
}
```

Do not load millions of records into memory.

---

# 49. FILTERING

Filtering must use validated fields.

Do not dynamically concatenate raw SQL from user input.

Whitelist:

```text
Allowed Fields
Allowed Operators
Allowed Sort Directions
```

---

# 50. SEARCH

Search implementations must use parameterized queries.

Avoid:

```text
Raw SQL concatenation
Unsafe dynamic expressions
Unbounded wildcard queries
```

Use appropriate database indexes.

---

# 51. SORTING

Never accept arbitrary SQL fragments from clients.

Instead:

```text
sortBy=name
sortDirection=asc
```

must be mapped against a known whitelist.

---

# 52. DATABASE MIGRATIONS

Use Alembic or the existing migration system.

Maintain:

```text
Migration
Rollback Strategy
Seed Data
Database Upgrade Documentation
```

Do not manually modify production schema without a controlled migration.

---

# 53. MIGRATION COMMAND DOCUMENTATION

Maintain an `.md` guide containing exact commands from the repository root.

For Alembic projects document commands such as:

```bash
alembic revision --autogenerate -m "Description"
alembic upgrade head
alembic downgrade -1
```

Only document commands actually verified against the project.

---

# 54. SEEDING

Seed only deterministic required data.

Never seed:

```text
Production passwords
Real customer information
Real payment credentials
Real secrets
```

Development credentials must be clearly marked.

---

# 55. REDIS

Redis may be used for:

```text
Caching
Rate Limiting
Idempotency
Distributed Locks
Temporary State
Session Support
```

Do not treat Redis as the primary source of truth unless explicitly designed.

---

# 56. RABBITMQ

Use RabbitMQ for event-driven communication where appropriate.

Preferred flow:

```text
Application
 ↓
Outbox
 ↓
Publisher
 ↓
RabbitMQ
 ↓
Consumer
```

Do not publish critical events before the corresponding database transaction is safely committed unless the architecture explicitly supports it.

---

# 57. EVENT CONTRACTS

Events should include stable metadata such as:

```text
EventId
EventType
Version
OccurredAt
CorrelationId
TraceId
TenantId
CompanyId
OrganizationId
Producer
Payload
```

Version event contracts when breaking changes occur.

---

# 58. EVENT CONSUMERS

Consumers must be:

```text
Idempotent
Retryable
Observable
Failure-aware
```

Assume duplicate delivery can happen.

---

# 59. DEAD LETTER QUEUE

Failed messages that exceed retry limits should move to an appropriate dead-letter mechanism.

Record:

```text
EventId
EventType
Reason
Exception
RetryCount
Timestamp
CorrelationId
```

---

# 60. OUTBOX PATTERN

For reliable event publishing:

```text
Database Transaction
 ↓
Business Data
+
Outbox Record
 ↓
Background Publisher
 ↓
RabbitMQ
```

The publisher should retry failed delivery.

---

# 61. BACKGROUND WORKERS

Use dedicated workers for:

```text
Long-running Processing
RabbitMQ Consumers
Scheduled Jobs
Heavy CPU Tasks
Bulk Processing
```

Do not block HTTP request threads with long-running work.

---

# 62. QUARTZ / SCHEDULING

If the platform requires Quartz-like scheduling, use an appropriate Python scheduler/worker architecture.

Possible technologies:

```text
APScheduler
Celery Beat
Task Queue Scheduler
External Scheduler
```

Select based on the actual deployment architecture.

Do not run duplicate schedulers across multiple replicas without distributed coordination.

---

# 63. CRON JOBS

Every scheduled task must consider:

```text
Idempotency
Duplicate Execution
Distributed Lock
Retry
Timeout
Failure Handling
Observability
Audit
```

---

# 64. JOB LOGGING

Every background job should log:

```text
Job Name
Execution Id
Start Time
End Time
Duration
Status
Exception
Retry Count
CorrelationId
TraceId
```

---

# 65. HEALTH CHECKS

Provide:

```text
GET /health/live
GET /health/ready
```

where appropriate.

Liveness:

```text
Process is alive
```

Readiness:

```text
Application can accept traffic
```

Do not make liveness fail because PostgreSQL is temporarily unavailable.

---

# 66. HEALTH DEPENDENCIES

Readiness may check:

```text
Database
Redis
RabbitMQ
Critical External Services
```

according to service requirements.

Return a graceful health response.

---

# 67. OPENAPI

FastAPI provides OpenAPI automatically.

Maintain accurate:

```text
Request Models
Response Models
Authentication
Authorization
Error Responses
Pagination
Filtering
Headers
Idempotency
```

Do not leave generated documentation misleading.

---

# 68. SCALAR

If Scalar is part of the platform standard, expose Scalar for API exploration.

Ensure production exposure follows the project's security policy.

---

# 69. API VERSIONING

Public APIs should use an explicit versioning strategy where breaking changes are possible.

Example:

```text
/api/v1/users
/api/v2/users
```

Do not silently break mobile or external clients.

---

# 70. API GATEWAY COMPATIBILITY

The FastAPI service must work behind:

```text
YARP
Ocelot
NGINX
Ingress
Load Balancer
API Gateway
```

Ensure:

```text
Forwarded Headers
HTTPS
Path Prefixes
Correlation
Authentication
```

are handled correctly.

---

# 71. OBSERVABILITY

Integrate:

```text
OpenTelemetry
Jaeger
Prometheus
Grafana
Serilog-compatible centralized logging where applicable
Seq
Kibana
Graylog
```

according to the platform environment.

Python-native structured logging may be used while preserving the platform log schema.

---

# 72. DISTRIBUTED TRACING

Trace:

```text
HTTP Request
 ↓
FastAPI
 ↓
Database
 ↓
Redis
 ↓
gRPC
 ↓
RabbitMQ
 ↓
External API
```

where instrumentation exists.

Propagate:

```text
TraceId
CorrelationId
```

through all supported boundaries.

---

# 73. STRUCTURED LOGGING

Use structured logs rather than only free-form strings.

Prefer fields:

```text
timestamp
service
environment
level
message
endpoint
method
file
line
correlationId
traceId
tenantId
userId
duration
exception
```

---

# 74. RUNTIME ERROR LOGS

Maintain:

```text
logs/runtime-error-logs/runtime-error-dd-mm-yy.txt
```

where local file logging is part of the platform requirement.

Each record should contain:

```text
Timestamp
Service
Entry Point
Endpoint
Method
File
File Location
Line
Root Cause
Exact Exception
Possible Solution
CorrelationId
TraceId
```

---

# 75. EXCEPTION LOGS

Maintain:

```text
logs/exception-logs/exception-logs-dd-mm-yy.txt
```

Include:

```text
Entry Point
Endpoint Name
Method Name
File Name
File Location
Line Number
Root Cause
Exact Exception Message
Possible Solution
Best Practice
Timestamp
CorrelationId
TraceId
```

Never fabricate a file or line number.

---

# 76. BUILD ERROR LOGS

Build/type/lint failures should be captured where automation supports it.

Use:

```text
logs/build-errors/build-error-dd-mm-yy.txt
```

Include:

```text
Timestamp
Project
File
Location
Line
Column
Exact Error
Root Cause
Possible Solution
```

---

# 77. QUERY LOGS

Maintain:

```text
logs/query-logs/query-dd-mm-yy.txt
```

Include:

```text
Endpoint
Service/Job Name
Method
File
File Location
Line
Database Provider
Generated Query
Started Time
Ending Time
Total Execution Time
CorrelationId
TraceId
```

Never log passwords or sensitive parameter values.

---

# 78. LOG ROTATION

Daily logs must not grow indefinitely.

Configure:

```text
Rotation
Retention
Compression
Centralized Export
```

according to deployment requirements.

---

# 79. FILE LOGGING VS CENTRALIZED LOGGING

Local files are useful for development and emergency diagnostics.

Production deployments should also support centralized collection.

Example:

```text
FastAPI
 ↓
OpenTelemetry / Structured Logger
 ↓
Collector
 ↓
Seq / Grafana / Kibana / Graylog
```

---

# 80. PERFORMANCE METRICS

Monitor:

```text
Request Count
Request Duration
Error Rate
CPU
Memory
Event Loop Lag
Database Duration
External Dependency Duration
Queue Depth
Worker Duration
```

---

# 81. REQUEST TIMING

Record:

```text
Start Time
End Time
Total Duration
```

for requests and important background operations.

Do not add expensive logging to every operation if it creates unacceptable overhead.

---

# 82. SECURITY HEADERS

Configure appropriate security headers at the application/gateway layer.

Consider:

```text
Content-Security-Policy
Strict-Transport-Security
X-Content-Type-Options
Referrer-Policy
Frame Protection
Permissions-Policy
```

Do not blindly copy a security configuration without verifying application compatibility.

---

# 83. CORS

CORS must be explicitly configured.

Never use:

```python
allow_origins=["*"]
```

for authenticated production APIs unless there is a documented architectural reason.

Review:

```text
Origins
Methods
Headers
Credentials
```

---

# 84. CSRF

For cookie-based authentication, implement appropriate CSRF protection.

For bearer-token APIs, review the actual authentication architecture rather than blindly adding incompatible CSRF mechanisms.

---

# 85. PASSWORD / OTP DATA

Never log:

```text
Password
OTP
Reset Token
Refresh Token
JWT
Security Answer
```

Mask or omit them completely.

---

# 86. SECURITY QUESTIONS

If the service participates in authentication flows involving security questions, use the centralized authentication service.

Do not independently implement:

```text
Password History
Security Questions
OTP Login
Forgot Password
Reset Password
```

inside unrelated business services.

---

# 87. PASSWORD HISTORY

The centralized authentication service must enforce rules such as:

```text
Last 3 passwords cannot be reused
```

where required by the platform security policy.

FastAPI services should trust the centralized authentication authority rather than duplicating password policy logic.

---

# 88. MODULE / PERMISSION MANAGEMENT

Business services must enforce permissions received from the centralized authorization system.

Possible dimensions:

```text
Module
Permission
Role
Tenant
Company
Organization
Resource
Action
```

---

# 89. FILE UPLOADS

Validate:

```text
Filename
Extension
MIME Type
Size
Content
Path
```

Never trust the client.

Avoid storing critical files on ephemeral container storage.

Use object storage where appropriate.

---

# 90. SSRF PROTECTION

When the service fetches user-provided URLs:

```text
Validate Scheme
Validate Host
Block Private Networks where appropriate
Block Metadata Endpoints
Restrict Redirects
Apply Timeout
```

Do not allow unrestricted server-side URL fetching.

---

# 91. SQL INJECTION

Always use:

```text
Parameterized Queries
ORM Expressions
Bound Parameters
```

Never concatenate untrusted user input into SQL.

---

# 92. COMMAND INJECTION

Never directly pass untrusted user input into:

```text
subprocess
shell
os.system
```

If operating-system commands are unavoidable:

```text
Whitelist
Validate
Avoid Shell
Restrict Permissions
```

---

# 93. SERIALIZATION

Never deserialize untrusted data using unsafe serialization mechanisms.

Prefer:

```text
JSON
Pydantic
Safe Binary Protocols
```

with validation.

---

# 94. DEPENDENCY INJECTION

FastAPI's dependency system should be used for:

```text
Database Session
Current User
Tenant Context
Authorization
Services
Repositories
Configuration
```

Avoid global mutable state.

---

# 95. DATABASE SESSION LIFECYCLE

Database sessions must be created and closed using a controlled lifecycle.

Do not use one global mutable session across requests.

Ensure rollback on failures.

---

# 96. CONNECTION POOLS

Configure database pools appropriately for:

```text
Development
Production
Container Replicas
Cloud Deployment
```

Account for the total number of application replicas.

---

# 97. N+1 QUERIES

Detect and prevent:

```text
1 query
+
N additional queries
```

Use appropriate:

```text
Eager Loading
Batching
Joins
Projection
Data Loader
```

where appropriate.

---

# 98. RESPONSE SIZE

Do not return unnecessary fields.

Use:

```text
DTO
Pagination
Projection
Field Selection
```

where appropriate.

---

# 99. API COMPATIBILITY

Assume clients may include:

```text
Angular
React
Next.js
MAUI
Kotlin Android
External Integrators
```

API contracts must therefore remain platform-neutral.

---

# 100. MOBILE CLIENT SUPPORT

Mobile clients require:

```text
Stable Contracts
Compact Responses
Pagination
Idempotency
Retry-Safe APIs
Consistent Error Codes
CorrelationId
```

Avoid breaking API changes.

---

# 101. BACKWARD COMPATIBILITY

When changing an API:

```text
Add
Deprecate
Migrate
Remove
```

rather than silently breaking existing clients.

---

# 102. TESTING

Follow:

```text
.ai/testing-and-performance.md
```

Use the existing project tooling.

Typical stack:

```text
pytest
pytest-asyncio
httpx
Testcontainers
```

where appropriate.

---

# 103. UNIT TESTS

Test:

```text
Domain Rules
Application Services
Validation
Permission Logic
Result Pattern
Repositories
Communication Adapters
```

---

# 104. API TESTS

Test:

```text
Authentication
Authorization
Validation
Result Pattern
Status Codes
Error Codes
CorrelationId
Idempotency
Rate Limiting
```

---

# 105. INTEGRATION TESTS

Use real infrastructure or containers where practical:

```text
PostgreSQL
Redis
RabbitMQ
```

Do not mock everything.

---

# 106. END-TO-END TESTS

Test critical workflows such as:

```text
Login
OTP
Password Reset
CRUD
Payment
Booking
Notification
Event Processing
Tenant Isolation
Permission Boundaries
```

where applicable.

---

# 107. LOAD TESTING

Maintain:

```text
tests/load-test/
```

Required performance tooling according to platform rules:

```text
NBomber
k6
Apache JMeter
```

Use the appropriate tool for the target.

Document:

```text
Run Command
Environment
Scenario
Virtual Users
Duration
Thresholds
Results
```

---

# 108. PYTHON LOAD TESTING

For FastAPI performance testing, measure:

```text
Requests/sec
Latency
p50
p95
p99
Error Rate
CPU
Memory
Database Duration
```

Do not judge performance from average latency alone.

---

# 109. DOCKER

Use multi-stage Docker builds where appropriate.

Production images should contain only required runtime dependencies.

---

# 110. NON-ROOT CONTAINER

Run the application as a non-root user where possible.

Do not require root privileges without justification.

---

# 111. Uvicorn / ASGI

Production deployment should use an appropriate ASGI server configuration.

Depending on deployment:

```text
Uvicorn
Gunicorn + Uvicorn Worker
Managed ASGI Runtime
```

Select the architecture appropriate for the current deployment environment and supported versions.

---

# 112. WORKER COUNT

Do not blindly increase worker count.

Consider:

```text
CPU
Memory
Database Connections
External Dependencies
Container Limits
```

The total database pool across all workers/replicas must remain within database capacity.

---

# 113. GRACEFUL SHUTDOWN

On shutdown:

```text
Stop accepting requests
Finish safe in-flight work
Close database pools
Close Redis
Close messaging connections
Flush telemetry
Flush logs
```

Do not silently discard critical work.

---

# 114. CI/CD

CI should verify:

```text
Dependency Installation
Lint
Formatting
Type Checking
Unit Tests
Integration Tests
API Tests
E2E Tests where appropriate
Build
Docker Build
Security Checks
```

---

# 115. DEPENDENCY SECURITY

Regularly scan Python dependencies.

Pay particular attention to:

```text
FastAPI
Starlette
Pydantic
Uvicorn
ORM
Database Drivers
Authentication Libraries
HTTP Clients
Serialization Libraries
```

Do not blindly upgrade production dependencies without testing.

---

# 116. PACKAGE MANAGEMENT

Prefer a modern reproducible dependency workflow.

Use the repository's existing standard:

```text
uv
Poetry
pip-tools
pip + requirements
```

Do not introduce a second package manager unnecessarily.

---

# 117. LOCK FILES

Production builds must use reproducible dependency versions.

Commit the appropriate lock file when the chosen dependency-management strategy supports it.

---

# 118. ENVIRONMENT CONFIGURATION

Use typed configuration.

For example:

```python
class Settings(BaseSettings):
    database_url: str
    redis_url: str
    rabbitmq_url: str
    environment: str
```

Do not scatter:

```text
os.getenv(...)
```

throughout business logic.

---

# 119. SECRET MANAGEMENT

Never commit:

```text
.env
Passwords
API Keys
Private Keys
Database Credentials
RabbitMQ Credentials
JWT Secrets
```

Use environment variables or a proper secret manager.

---

# 120. ENVIRONMENT SEPARATION

Support clear configuration for:

```text
Development
Testing
Staging
Production
```

Do not use production credentials during local development.

---

# 121. API DOCUMENTATION

FastAPI OpenAPI documentation must accurately describe:

```text
Endpoints
Request Models
Response Models
Authentication
Errors
Pagination
Filtering
Headers
Idempotency
```

---

# 122. PROGRAMMER GUIDE

Maintain:

```text
docs/programmers-guide/
```

with concise documentation for:

```text
FastAPI Architecture
Project Structure
Creating CRUD
Creating Entity
Creating CQRS
Validation
Dependency Injection
Repository
Unit of Work
Database Migration
HTTP
gRPC
RabbitMQ
Background Worker
Scheduled Jobs
Testing
Load Testing
Troubleshooting
Deployment
```

---

# 123. CRUD GUIDE

Document the exact project workflow for creating a new CRUD:

```text
Entity
 ↓
Migration
 ↓
Repository
 ↓
Application Service
 ↓
DTO
 ↓
Validation
 ↓
Router
 ↓
Authorization
 ↓
Tests
 ↓
Documentation
```

Commands must be based on the actual project.

---

# 124. BUILD ERROR DOCUMENTATION

The programmer guide must explain:

```text
How to identify build errors
Where build logs are stored
How to identify file/line
How to reproduce
How to fix
```

---

# 125. DATABASE COMMAND DOCUMENTATION

Provide an `.md` file from the repository root explaining exact commands for:

```text
Create Migration
Apply Migration
Rollback Migration
Seed Database
Reset Development Database
```

Only include verified commands.

---

# 126. TROUBLESHOOTING

Document common failures:

```text
Database unavailable
Redis unavailable
RabbitMQ unavailable
Port occupied
Migration failure
Import failure
Dependency failure
Environment configuration failure
Docker failure
Authentication failure
Permission failure
```

Each should include:

```text
Symptom
Root Cause
Diagnosis
Solution
```

---

# 127. REUSABILITY

The FastAPI implementation must remain reusable across:

```text
HRM
ERP
Accounting
HMS
Payroll
Ticketing
Booking
Payment
Payment Gateway
Notification
SaaS
Transport
Inventory
POS
```

Do not hardcode one business domain into shared infrastructure.

---

# 128. DOMAIN INDEPENDENCE

Framework code should not leak into domain logic.

Avoid importing:

```python
from fastapi import Request
```

into domain entities or domain services.

Keep framework dependencies at the outer layers.

---

# 129. APPLICATION LAYER

Application services coordinate:

```text
Validation
Authorization
Transactions
Repositories
Domain Operations
Events
Result Mapping
```

They should not contain infrastructure-specific implementation details.

---

# 130. DOMAIN LAYER

Domain logic should remain independent from:

```text
FastAPI
SQLAlchemy
RabbitMQ
Redis
HTTP
gRPC
```

where practical.

---

# 131. INFRASTRUCTURE ADAPTERS

Infrastructure implementations belong behind interfaces.

Examples:

```text
IUserRepository
IEmailProvider
INotificationProvider
IPaymentProvider
ICommunicationProvider
IEventPublisher
ICacheProvider
```

Implement adapters:

```text
PostgresRepository
MySqlRepository
HttpCommunicationProvider
GrpcCommunicationProvider
RabbitMqEventPublisher
RedisCacheProvider
```

where actually required.

---

# 132. PROVIDER SELECTION

Provider selection should be configuration-driven.

Examples:

```text
DATABASE_PROVIDER=postgres
COMMUNICATION_PROVIDER=grpc
CACHE_PROVIDER=redis
MESSAGE_PROVIDER=rabbitmq
```

Changing a provider should not require rewriting domain/application logic.

---

# 133. PROVIDER REALITY RULE

Do not create fake "universal" abstractions.

If two providers have fundamentally different capabilities:

```text
PostgreSQL
MongoDB
SQLite
```

the abstraction must expose only genuinely common behavior.

Provider-specific features belong in provider-specific adapters.

---

# 134. NO FAKE IMPLEMENTATIONS

Never create:

```text
pass
TODO
NotImplementedError
FakeRepository
FakePaymentProvider
FakeRabbitMq
```

as a substitute for production functionality unless explicitly required for tests.

---

# 135. NO TECHNICAL DEBT

Do not knowingly introduce:

```text
Duplicate Logic
Dead Code
Magic Strings
Unbounded Retries
Hardcoded Secrets
God Classes
God Routers
Uncontrolled Global State
```

---

# 136. PERFORMANCE BEFORE OPTIMIZATION

Do not prematurely optimize.

First ensure:

```text
Correctness
Security
Observability
Maintainability
```

Then optimize using measured evidence.

---

# 137. FINAL PRODUCTION CHECKLIST

Before declaring the FastAPI service complete:

```text
[ ] Latest supported Python selected
[ ] Latest compatible FastAPI selected
[ ] Existing architecture understood
[ ] Clean architecture preserved
[ ] Domain isolated
[ ] Application layer implemented
[ ] Infrastructure separated
[ ] Dependency injection implemented
[ ] Pydantic validation implemented
[ ] Result Pattern implemented
[ ] Centralized exception handling implemented
[ ] Graceful dependency errors implemented
[ ] Bangla/English localization supported
[ ] Future localization supported
[ ] Authentication verified
[ ] Authorization verified
[ ] Tenant isolation verified
[ ] Company/Organization context verified
[ ] Module/Permission checks verified
[ ] CorrelationId verified
[ ] TraceId verified
[ ] IP tracing verified
[ ] Rate limiting verified
[ ] Idempotency verified
[ ] Retry policy verified
[ ] Circuit breaker verified
[ ] Timeout verified
[ ] HTTP communication verified
[ ] gRPC communication verified
[ ] RabbitMQ communication verified
[ ] YARP compatibility verified
[ ] Ocelot compatibility verified
[ ] Database abstraction verified
[ ] PostgreSQL verified
[ ] Provider switching verified where required
[ ] Repository verified
[ ] Unit of Work verified where required
[ ] Optimistic concurrency verified
[ ] Soft delete verified
[ ] Audit logging verified
[ ] Query logging verified
[ ] Runtime error logging verified
[ ] Exception logging verified
[ ] Build error logging verified
[ ] OpenTelemetry verified
[ ] Metrics verified
[ ] Distributed tracing verified
[ ] Health endpoints verified
[ ] OpenAPI verified
[ ] Scalar verified where required
[ ] Redis verified
[ ] Outbox verified where required
[ ] Background workers verified
[ ] Scheduled jobs verified
[ ] Migration commands documented
[ ] Programmer Guide updated
[ ] Unit tests pass
[ ] Integration tests pass
[ ] API tests pass
[ ] E2E tests pass where required
[ ] NBomber load tests available where applicable
[ ] k6 load/stress tests available
[ ] JMeter performance tests available
[ ] Docker build verified
[ ] Non-root container verified
[ ] Graceful shutdown verified
[ ] CI/CD verified
[ ] Security review completed
[ ] Documentation verified
[ ] Professional Git commit created
```

---

# 138. FINAL ARCHITECTURE HIERARCHY

The rule hierarchy is:

```text
.ai/MASTER-RULE.md
        ↓
.ai/AI_RULES.md
        ↓
.ai/communication.md
.ai/observability.md
.ai/testing-and-performance.md
        ↓
.ai/backend/python-fastapi.md
        ↓
Python + FastAPI Service
```

Common platform rules remain centralized.

This document defines only Python/FastAPI implementation-specific behavior.

The resulting service must be:

```text
Production-ready
Enterprise-grade
Secure
Observable
Scalable
Testable
Maintainable
Reusable
Multi-tenant
Communication-provider independent
Database-provider independent where required
API-platform independent
Mobile-client compatible
```

# END OF PYTHON + FASTAPI ENTERPRISE ENGINEERING RULES
