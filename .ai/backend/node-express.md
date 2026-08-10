# Node.js + Express Enterprise Engineering Rules

## 1. PURPOSE

This document defines Express-specific implementation rules for enterprise Node.js services.

It extends:

```text
.ai/backend/node.md
.ai/MASTER-RULE.md
.ai/AI_RULES.md
.ai/communication.md
.ai/observability.md
.ai/testing-and-performance.md
```

The common Node.js rules are authoritative.

Do NOT duplicate or contradict them.

This document only defines how those platform rules are implemented using:

```text
Node.js
+
TypeScript
+
Express
```

---

# 2. VERSION POLICY

Always use the latest stable and supported versions available at implementation time.

Do not permanently hardcode:

```text
Node.js version
Express version
TypeScript version
```

Inspect the existing project first.

Prefer the newest compatible stable versions.

Never downgrade dependencies merely to make implementation easier.

---

# 3. EXISTING PROJECT FIRST

Before changing anything inspect:

```text
package.json
tsconfig.json
eslint configuration
prettier configuration
Dockerfile
docker-compose files
environment configuration
src/
tests/
docs/
existing middleware
existing routes
existing controllers
existing services
existing repositories
```

Understand the current architecture before modifying it.

Reuse existing conventions.

Do not redesign unrelated code.

---

# 4. RECOMMENDED EXPRESS STRUCTURE

For enterprise services, prefer a structure similar to:

```text
src/
├── domain/
│   ├── entities/
│   ├── value-objects/
│   ├── services/
│   └── events/
│
├── application/
│   ├── commands/
│   ├── queries/
│   ├── handlers/
│   ├── dto/
│   ├── validators/
│   └── interfaces/
│
├── infrastructure/
│   ├── database/
│   ├── repositories/
│   ├── messaging/
│   ├── grpc/
│   ├── http/
│   ├── cache/
│   └── telemetry/
│
├── interfaces/
│   └── http/
│       ├── controllers/
│       ├── routes/
│       ├── middleware/
│       └── presenters/
│
├── workers/
├── jobs/
├── config/
└── shared/
```

Adapt this to the existing project.

Do not create unnecessary layers.

---

# 5. EXPRESS APPLICATION BOOTSTRAPPING

Separate application construction from process startup.

Prefer:

```text
app.ts
server.ts
```

Conceptually:

```text
app.ts
→ Express configuration
→ middleware
→ routes
→ error handling

server.ts
→ configuration validation
→ server startup
→ graceful shutdown
```

This makes testing easier.

---

# 6. APPLICATION CREATION

Avoid hiding the entire application inside a single giant startup file.

Prefer:

```typescript
const app = createApp();

startServer(app);
```

This allows integration tests to import the application without starting a network listener.

---

# 7. EXPRESS MIDDLEWARE ORDER

Middleware order matters.

A typical production pipeline is:

```text
Request
 ↓
Proxy / IP Configuration
 ↓
Request ID / Correlation ID
 ↓
Tracing
 ↓
Security Headers
 ↓
CORS
 ↓
Request Logging
 ↓
Body Parsing
 ↓
Rate Limiting
 ↓
Authentication
 ↓
Authorization
 ↓
Validation
 ↓
Routes
 ↓
404 Handler
 ↓
Central Error Handler
```

Adjust ordering according to actual requirements.

Never blindly copy this order if an existing architecture requires something different.

---

# 8. REQUEST ID

Every request must have a correlation mechanism.

Use:

```text
CorrelationId
TraceId
```

If the platform provides a valid correlation ID, propagate it according to platform rules.

Otherwise generate one.

Attach it to the request context and logging context.

Example:

```typescript
req.correlationId
```

or an equivalent request-context abstraction.

Avoid scattering correlation-ID logic across controllers.

---

# 9. ASYNC CONTROLLERS

Async route handlers must correctly propagate rejected promises to the centralized error handler.

Do not allow:

```typescript
async (req, res) => {
    throw new Error("failure");
}
```

to become an unhandled rejection because of incorrect Express integration.

Use the project's established async-handler abstraction or Express-supported approach.

---

# 10. CONTROLLERS

Controllers should be thin.

Controller responsibilities:

```text
Receive request
 ↓
Extract validated input
 ↓
Call application use case
 ↓
Map result
 ↓
Return HTTP response
```

Controllers must NOT contain:

```text
Business Rules
Complex Database Queries
RabbitMQ Publishing Logic
Large Validation Logic
External API Logic
Transaction Orchestration
```

Move those responsibilities into appropriate application/infrastructure components.

---

# 11. ROUTES

Routes should define HTTP composition.

Example conceptual structure:

```text
routes/
├── notification.routes.ts
├── payment.routes.ts
├── user.routes.ts
└── health.routes.ts
```

Avoid placing business logic inside route declarations.

---

# 12. API VERSIONING

Enterprise APIs should support versioning where required.

Examples:

```text
/api/v1/notifications
/api/v2/notifications
```

or the project's established versioning mechanism.

Never break an existing public contract silently.

---

# 13. HTTP METHODS

Follow standard semantics:

```text
GET
POST
PUT
PATCH
DELETE
```

Use them consistently.

Do not use:

```text
POST
```

for every operation merely because it is convenient.

---

# 14. HTTP STATUS CODES

Use appropriate HTTP semantics.

Examples:

```text
200 OK
201 Created
202 Accepted
204 No Content

400 Bad Request
401 Unauthorized
403 Forbidden
404 Not Found
409 Conflict
422 Unprocessable Entity
429 Too Many Requests

500 Internal Server Error
502 Bad Gateway
503 Service Unavailable
504 Gateway Timeout
```

Do not expose arbitrary status codes without a reason.

---

# 15. RESULT PATTERN

Express controllers must use the platform Result Pattern.

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

Multiple errors must be returned where possible.

Do not force frontend developers to make multiple requests simply to discover independent validation failures.

---

# 16. CENTRALIZED ERROR HANDLER

Express must have one centralized error-handling middleware.

Conceptually:

```typescript
app.use(errorHandler);
```

It must be registered after routes.

The error handler must:

```text
Classify Error
 ↓
Log Technical Details
 ↓
Map Error to Safe API Response
 ↓
Attach TraceId/CorrelationId
 ↓
Return Correct HTTP Status
```

Never expose:

```text
Stack Trace
Database Password
Connection String
Internal File System
Secrets
Internal Infrastructure Details
```

to API clients.

---

# 17. EXPRESS ERROR TYPES

Create centralized application error types where appropriate.

Examples:

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

Do not create dozens of meaningless custom errors.

---

# 18. 404 HANDLING

Unknown routes must produce the centralized Result Pattern.

Example conceptual response:

```json
{
  "success": false,
  "message": "The requested resource was not found.",
  "errors": [
    {
      "code": "ROUTE_NOT_FOUND",
      "field": null,
      "message": "Endpoint does not exist."
    }
  ],
  "traceId": "..."
}
```

Do not leak route internals.

---

# 19. REQUEST VALIDATION

Validate incoming requests before business processing.

Validate:

```text
Params
Query
Headers
Body
```

Use the project's validation library.

Possible libraries:

```text
Zod
Joi
Yup
class-validator
```

Do not introduce multiple validation frameworks unnecessarily.

---

# 20. VALIDATION SCHEMAS

Keep validation schemas separate from controllers where they become substantial.

Example:

```text
validators/
├── create-notification.schema.ts
├── update-notification.schema.ts
└── search-notification.schema.ts
```

Validation should be reusable.

---

# 21. REQUEST DTOs

Do not pass raw Express request objects deep into the application layer.

Avoid:

```typescript
service.execute(req);
```

Prefer:

```typescript
service.execute({
    userId,
    tenantId,
    notificationId
});
```

This keeps the application independent from Express.

---

# 22. RESPONSE MAPPING

Controllers should map application results into HTTP responses.

Avoid returning infrastructure entities directly.

Prefer:

```text
Database Entity
 ↓
Domain/Application Result
 ↓
Response DTO
 ↓
HTTP JSON
```

This prevents database schema from becoming the public API contract.

---

# 23. SERIALIZATION

Do not blindly serialize entire domain/database objects.

Explicitly select fields.

This prevents accidental exposure of:

```text
Password Hash
Internal IDs
Secrets
Internal Metadata
Audit Data
Sensitive Fields
```

---

# 24. PAGINATION

Express query parameters must be validated.

Example:

```text
?page=1&pageSize=25
```

Enforce:

```text
minimum page size
maximum page size
valid numeric values
```

Never allow:

```text
?pageSize=999999999
```

to become an unlimited database query.

---

# 25. FILTERING

Filter parameters must be explicitly allowed.

Never convert arbitrary query-string keys into database filters.

Bad:

```typescript
repository.find(req.query);
```

Prefer an explicit filter DTO.

---

# 26. SEARCH

Search input must be:

```text
Validated
Bounded
Parameterized
```

Apply appropriate database indexes.

---

# 27. AUTHENTICATION MIDDLEWARE

Authentication belongs at the HTTP boundary.

Conceptually:

```text
Request
 ↓
Authentication Middleware
 ↓
Verified User Context
 ↓
Authorization Middleware
 ↓
Controller
```

Do not decode JWT payloads and treat them as trusted without validating the token.

---

# 28. AUTHORIZATION MIDDLEWARE

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
```

where applicable.

Never rely only on Angular/React/Next.js authorization.

---

# 29. REQUEST CONTEXT

Create a consistent request context containing where applicable:

```text
UserId
TenantId
CompanyId
OrganizationId
CorrelationId
TraceId
IP
Roles
Permissions
```

Do not read these values from arbitrary headers after authentication has already established trusted identity.

---

# 30. MULTI-TENANT EXPRESS SERVICES

Every tenant-aware endpoint must enforce tenant isolation.

Example conceptual flow:

```text
JWT
 ↓
Authentication
 ↓
Tenant Context
 ↓
Application Service
 ↓
Repository
 ↓
WHERE TenantId = authenticatedTenant
```

Never trust:

```text
?tenantId=
```

as the sole source of tenant identity.

---

# 31. IDEMPOTENCY MIDDLEWARE

Critical write endpoints may require:

```text
Idempotency-Key
```

The middleware/application layer must:

```text
Validate Key
 ↓
Determine Scope
 ↓
Check Existing Request
 ↓
Return Previous Result OR Execute
 ↓
Persist Result
```

Do not merely accept the header.

---

# 32. RATE LIMITING

Express APIs must use centralized rate limiting where required.

Support limits based on:

```text
IP
User
Tenant
Company
API Key
Endpoint
```

Take reverse proxies into account.

Configure trusted proxies correctly.

---

# 33. TRUST PROXY

Do not blindly enable:

```typescript
app.set("trust proxy", true);
```

unless the deployment architecture requires it.

Configure trusted proxy behavior according to:

```text
Docker
Kubernetes
YARP
Ocelot
Ingress
Load Balancer
```

This is particularly important for:

```text
IP
Rate Limiting
Audit
Security
```

---

# 34. CORS

Configure CORS explicitly.

Do not use:

```text
origin: "*"
```

for authenticated production APIs unless intentionally required.

Allowed origins should come from trusted configuration.

---

# 35. SECURITY HEADERS

Use an established security middleware such as the project's approved equivalent.

Configure appropriate:

```text
CSP
HSTS
X-Content-Type-Options
Frame Protection
Referrer Policy
```

Do not blindly enable policies that break legitimate application behavior.

---

# 36. BODY SIZE LIMITS

Set explicit limits for:

```text
JSON
URL Encoded
Multipart
File Upload
```

Never allow unbounded request bodies.

---

# 37. FILE UPLOADS

For Express upload endpoints:

```text
Validate size
Validate MIME
Validate extension
Sanitize filename
Prevent path traversal
Store outside executable directories
Scan when required
```

Never trust the client-provided MIME type.

---

# 38. HTTP CLIENTS

Do not call external services directly from controllers.

Prefer:

```text
Controller
 ↓
Application Service
 ↓
External Service Interface
 ↓
HTTP Client Adapter
```

HTTP client implementation must support:

```text
Timeout
Retry
Circuit Breaker
CorrelationId
TraceId
Authentication
Idempotency
```

where applicable.

---

# 39. gRPC CLIENTS

gRPC calls must be hidden behind application/infrastructure abstractions.

Do not place generated gRPC client calls inside controllers.

Propagate:

```text
CorrelationId
TraceId
Authentication Context
Deadline
```

where supported.

---

# 40. RABBITMQ

Express services consuming/publishing RabbitMQ messages must not couple business logic directly to RabbitMQ client APIs.

Prefer:

```text
Application
 ↓
IEventPublisher
 ↓
RabbitMQ Adapter
```

Consumers:

```text
RabbitMQ
 ↓
Consumer Adapter
 ↓
Application Handler
```

---

# 41. MESSAGE IDEMPOTENCY

Every critical RabbitMQ consumer must assume duplicate delivery can occur.

Implement appropriate:

```text
MessageId
EventId
Inbox
Deduplication
```

strategy.

Never assume exactly-once delivery.

---

# 42. OUTBOX

When a database transaction and event publication must be reliable:

```text
Application Command
 ↓
Database Transaction
 ├── Business Data
 └── Outbox Event
 ↓
Commit
 ↓
Outbox Worker
 ↓
RabbitMQ
```

Do not publish a critical event first and assume the database transaction will succeed.

---

# 43. BACKGROUND WORKERS

Do not run long-running background loops inside an Express request handler.

Use:

```text
Worker Process
Queue Consumer
Scheduler
Job Runner
```

where appropriate.

Workers must support:

```text
Graceful Shutdown
Retry
Logging
Metrics
Idempotency
Concurrency Control
```

---

# 44. QUARTZ EQUIVALENT

Quartz.NET is not an Express/Node.js framework.

For Node.js use the project's selected scheduler.

Possible:

```text
node-cron
BullMQ
Agenda
External Scheduler
```

If the platform requires Quartz-like scheduling semantics, implement those semantics through the approved Node scheduler rather than attempting to use Quartz.NET directly.

Document:

```text
Job Name
Cron
Timezone
Concurrency
Retry
Failure Handling
Manual Trigger
Monitoring
```

---

# 45. HEALTH ENDPOINTS

Provide separate concepts where appropriate:

```text
GET /health/live
GET /health/ready
```

Liveness:

```text
Is the process alive?
```

Readiness:

```text
Can the service safely receive traffic?
```

Dependency health may include:

```text
Database
Redis
RabbitMQ
External APIs
```

Do not make liveness fail merely because a temporary external dependency is unavailable.

---

# 46. OPENAPI

Expose OpenAPI documentation for HTTP APIs where required.

Document:

```text
Authentication
Authorization
Request
Response
Errors
Pagination
Filtering
Idempotency
Rate Limits
```

Use the project's approved tooling.

Do not manually maintain contradictory API documentation.

---

# 47. SCALAR

If the platform standard requires Scalar:

```text
OpenAPI
 ↓
Scalar
```

Use the actual project configuration.

Do not introduce a second API documentation UI without reason.

---

# 48. OBSERVABILITY

Express-specific instrumentation must integrate with:

```text
OpenTelemetry
Serilog-equivalent structured logging
Metrics
Distributed Tracing
```

as defined by:

```text
.ai/observability.md
```

Capture:

```text
HTTP Request
HTTP Response
Duration
Status
Route
Method
CorrelationId
TraceId
```

Do not log sensitive request bodies indiscriminately.

---

# 49. REQUEST LOGGING

Log requests using structured fields.

Useful fields:

```text
Timestamp
Method
Route
StatusCode
Duration
Service
CorrelationId
TraceId
UserId
TenantId
```

Mask:

```text
Authorization
Cookie
Password
OTP
Tokens
Secrets
```

---

# 50. QUERY LOGGING

Database instrumentation should integrate with:

```text
logs/query-logs/
```

according to the centralized observability rules.

Capture where supported:

```text
Service
Endpoint
Method
File
Line
Database Provider
Query
Start
End
Duration
CorrelationId
TraceId
```

Never expose secrets embedded in query parameters.

---

# 51. EXCEPTION LOGGING

Centralized Express error middleware must feed the centralized exception logging mechanism.

Expected diagnostic context:

```text
Entry Point
Endpoint
Method
File
File Location
Line
Root Cause
Exact Exception Message
Possible Solution
Best Practice
CorrelationId
TraceId
```

Do not fabricate source locations.

If runtime source maps are available, use them to map compiled JavaScript locations to TypeScript sources.

---

# 52. SOURCE MAPS

For production diagnostics, configure source maps appropriately.

The goal is to allow:

```text
compiled JavaScript
        ↓
original TypeScript
        ↓
actual file
        ↓
line
        ↓
developer fix
```

Do not expose source maps publicly unless intentionally required.

---

# 53. GRACEFUL DEPENDENCY FAILURE

If a dependency is unavailable:

```text
Database
Redis
RabbitMQ
External API
gRPC Service
```

the API should return a graceful user-facing message.

Example:

```text
The notification service is temporarily unavailable. Please try again later.
```

Technical details belong in runtime/exception logs.

Logs must contain:

```text
Actual Dependency
Actual Exception
Timestamp
Endpoint/Job
File
Line
Root Cause
Possible Solution
```

---

# 54. DATABASE ERRORS

Do not return:

```text
Postgres connection refused
MySQL authentication failed
MongoDB server unavailable
SQL syntax error near...
```

directly to clients.

Map database failures to safe application errors.

Log the actual technical error internally.

---

# 55. EXPRESS AS A MICROSERVICE

Express services must remain independently deployable.

A service should have:

```text
Own Configuration
Own Database Boundary
Own API
Own Tests
Own Docker Image
Own Health Checks
Own Observability
Own CI/CD
```

Do not create hidden shared database dependencies between services.

---

# 56. SERVICE COMMUNICATION

Supported communication mechanisms may include:

```text
HTTP
gRPC
RabbitMQ
```

Gateway mechanisms may include:

```text
YARP
Ocelot
```

The service must use the communication abstraction defined by:

```text
.ai/communication.md
```

Provider switching must not require business-logic rewrites.

---

# 57. DATABASE ABSTRACTION

If database provider switching is required:

```text
DatabaseProvider=postgres
```

could conceptually select:

```text
PostgreSQL
MySQL
SQL Server
Oracle
SQLite
MongoDB
```

Use a factory/provider abstraction.

Do not pretend provider-specific features are universally portable.

Provider-specific behavior belongs behind adapters.

---

# 58. CONFIGURATION

Configuration must come from environment/configuration providers.

Example:

```text
NODE_ENV
PORT
DATABASE_PROVIDER
DATABASE_URL
REDIS_URL
RABBITMQ_URL
OTEL_ENDPOINT
```

Never hardcode production infrastructure.

Validate required configuration during startup.

Fail fast for invalid mandatory configuration.

---

# 59. CONFIGURATION VALIDATION

Startup must identify invalid configuration clearly.

Example:

```text
DATABASE_URL is required.
RABBITMQ_URL is required.
OTEL_ENDPOINT is invalid.
```

Do not start a service that is guaranteed to fail immediately because required configuration is missing.

---

# 60. ENVIRONMENT MANAGEMENT

Support:

```text
Development
Test
Staging
Production
```

without embedding environment-specific business logic.

Never commit production secrets.

---

# 61. GRACEFUL SHUTDOWN

Express server shutdown should:

```text
Stop accepting traffic
 ↓
Allow active requests to finish
 ↓
Stop consumers/workers
 ↓
Close RabbitMQ
 ↓
Close Redis
 ↓
Close Database
 ↓
Flush telemetry/logs
 ↓
Close HTTP server
```

Use appropriate timeout limits.

Never wait forever.

---

# 62. ERROR DURING STARTUP

Startup failures should:

```text
Log exact reason
Identify configuration/dependency
Provide possible solution
Exit with non-zero status
```

Example:

```text
Database connection failed.

Root Cause:
PostgreSQL connection refused.

Possible Solution:
Verify PostgreSQL availability and DATABASE_URL.
```

Never pretend startup succeeded.

---

# 63. EXPRESS PERFORMANCE

Avoid middleware that unnecessarily processes every request.

Measure before optimizing.

Watch:

```text
Event Loop Lag
Memory
CPU
Request Duration
Database Duration
External API Duration
Serialization
Payload Size
```

---

# 64. EVENT LOOP SAFETY

Avoid synchronous operations in request paths:

```text
fs.readFileSync
fs.writeFileSync
CPU-heavy loops
Large synchronous JSON operations
```

unless there is a justified reason.

Use asynchronous APIs.

Use workers for CPU-intensive operations.

---

# 65. RESPONSE COMPRESSION

Use compression only when beneficial.

Consider:

```text
Payload Size
CPU Cost
Gateway Compression
Already Compressed Content
```

Do not double-compress unnecessarily.

---

# 66. REQUEST TIMEOUTS

Every API must have sensible request timeout behavior.

Do not allow requests to remain open indefinitely.

Timeouts must integrate with downstream:

```text
HTTP
gRPC
Database
RabbitMQ
```

timeouts.

---

# 67. DATABASE CONNECTION LIFECYCLE

Initialize database infrastructure once per process where appropriate.

Do not create a new database connection for every request when the selected driver/ORM supports pooling.

Close resources during graceful shutdown.

---

# 68. REDIS

Use Redis through an abstraction where provider switching/reuse is required.

Potential uses:

```text
Caching
Distributed Locks
Idempotency
Rate Limiting
Sessions
Temporary State
```

Do not treat Redis as the authoritative database unless explicitly designed that way.

---

# 69. CACHE INVALIDATION

Whenever caching business data:

```text
Define TTL
Define Invalidation
Define Stale Behavior
Define Failure Behavior
```

Never introduce caching without deciding how stale data is handled.

---

# 70. DISTRIBUTED LOCKS

When multiple service instances execute the same scheduled work, distributed locking may be required.

Do not assume:

```text
One container
=
One job execution
```

Production deployments commonly run multiple replicas.

---

# 71. AUDIT LOGGING

Critical Express operations should use the centralized audit mechanism.

Record where appropriate:

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
```

Never log credentials.

---

# 72. API GATEWAY COMPATIBILITY

Express services must work correctly behind:

```text
YARP
Ocelot
NGINX
Ingress
Cloud Load Balancer
```

Ensure:

```text
Forwarded Headers
CorrelationId
TraceId
Client IP
HTTPS Detection
```

are handled according to trusted infrastructure.

---

# 73. HTTP CLIENT RETRIES

Only retry operations that are safe to retry.

GET is generally easier to retry.

POST must use explicit idempotency semantics where retrying can cause duplicate side effects.

Never blindly retry every request.

---

# 74. CIRCUIT BREAKER

External dependency adapters should support circuit breaking where required.

Example:

```text
Express Service
 ↓
HTTP Client
 ↓
Circuit Breaker
 ↓
External Service
```

When open, fail fast with a graceful application error.

---

# 75. CORRELATION PROPAGATION

Propagate correlation information through:

```text
Express
 ↓
HTTP
 ↓
gRPC
 ↓
RabbitMQ
 ↓
Worker
```

The same trace should remain searchable across services.

---

# 76. TESTING

Express applications must follow:

```text
.ai/testing-and-performance.md
```

Typical stack:

```text
Vitest/Jest
+
Supertest
+
Testcontainers
```

Use whichever testing stack the project already standardizes on.

---

# 77. UNIT TESTS

Test:

```text
Controllers
Application Services
Validators
Error Mapping
Authorization
Business Rules
```

Controllers should be tested without requiring the entire production infrastructure where unit isolation is appropriate.

---

# 78. API INTEGRATION TESTS

Use Supertest or the project's equivalent to verify:

```text
Routes
Middleware
Validation
Authentication
Authorization
Error Handling
Result Pattern
Status Codes
Response Contracts
```

---

# 79. DATABASE INTEGRATION TESTS

Test against a realistic database engine where possible.

Do not rely exclusively on mocks for critical persistence behavior.

Verify:

```text
Queries
Transactions
Migrations
Concurrency
Indexes
Constraints
Soft Delete
Tenant Isolation
```

---

# 80. LOAD TESTS

Maintain:

```text
tests/load-test/
```

where required.

Required tooling according to platform rules:

```text
NBomber
k6
Apache JMeter
```

Document commands and result locations.

Do not confuse unit tests with performance tests.

---

# 81. DOCKER

Use multi-stage Docker builds where appropriate.

Typical flow:

```text
Node Build Image
 ↓
npm ci
 ↓
npm run build
 ↓
Production Image
 ↓
Copy compiled application
 ↓
Install production dependencies
 ↓
Run application
```

Do not ship unnecessary build tools in production images.

---

# 82. NON-ROOT CONTAINER

Production containers should run as a non-root user where compatible with the application.

Never require root privileges without justification.

---

# 83. CI/CD

CI/CD should verify:

```text
Install
Type Check
Lint
Unit Tests
Integration Tests
Build
Security Audit
Docker Build
```

Performance tests may execute in dedicated pipelines/environments.

---

# 84. MIGRATIONS

Migration commands must be documented in:

```text
docs/programmers-guide/
```

The exact command must match the project's ORM.

Do not invent commands.

Include:

```text
Create Migration
Run Migration
Rollback where supported
Production Migration Strategy
```

---

# 85. PROGRAMMER GUIDE

For every major Express service maintain documentation for:

```text
Architecture
Folder Structure
CRUD
Entity
CQRS
Validation
Repository
Migration
Middleware
Authentication
Authorization
HTTP
gRPC
RabbitMQ
Background Worker
Scheduled Job
Testing
Load Testing
Troubleshooting
```

Keep examples aligned with the actual implementation.

---

# 86. NO BUSINESS LOGIC IN MIDDLEWARE

Middleware may handle:

```text
Authentication
Correlation
Tracing
Security
Rate Limiting
Validation
```

Business operations belong in application/domain layers.

Avoid middleware such as:

```text
calculatePayment()
createNotification()
issueTicket()
```

unless it is genuinely request infrastructure.

---

# 87. NO BUSINESS LOGIC IN ROUTES

Avoid:

```typescript
router.post("/payments", async (req, res) => {
    // 200 lines of business logic
});
```

Prefer:

```text
Route
 ↓
Controller
 ↓
Use Case
 ↓
Domain
 ↓
Infrastructure
```

---

# 88. NO DATABASE ACCESS IN CONTROLLERS

Avoid:

```typescript
router.get("/", async (req, res) => {
    const users = await db.user.findMany();
});
```

Prefer:

```text
Controller
 ↓
Application Service
 ↓
Repository
 ↓
Database
```

---

# 89. NO RAW EXPRESS TYPES IN DOMAIN

Do not import:

```typescript
import { Request } from "express";
```

into domain logic.

Express belongs at the interface boundary.

---

# 90. LOGGING SAFETY

Never log:

```text
Authorization Header
Cookies
Password
OTP
Refresh Token
Access Token
Private Key
API Secret
Database Password
Payment Card Data
```

Use masking/redaction.

---

# 91. ERROR RESPONSE CONSISTENCY

All endpoints must follow the same error contract.

Do not create endpoint-specific error formats.

Frontend teams should be able to consume every Express service using the same response model.

---

# 92. API CONTRACT COMPATIBILITY

Before changing:

```text
Request fields
Response fields
Status codes
Error codes
Authentication
Pagination
```

check consumers.

Breaking changes require versioning or migration.

---

# 93. DEPENDENCY FAILURE BEHAVIOR

When a downstream service fails:

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

Do not expose internal connection details.

---

# 94. PRODUCTION READINESS CHECKLIST

Before declaring an Express service complete:

```text
[ ] Latest supported Node.js selected
[ ] TypeScript strictness verified
[ ] Express configuration verified
[ ] Middleware order reviewed
[ ] Centralized error handler implemented
[ ] Result Pattern implemented
[ ] Validation implemented
[ ] Authentication verified
[ ] Authorization verified
[ ] Tenant isolation verified
[ ] CorrelationId verified
[ ] TraceId verified
[ ] Idempotency verified where required
[ ] Rate limiting verified
[ ] IP/proxy configuration verified
[ ] CORS verified
[ ] Security headers verified
[ ] Request limits verified
[ ] Database verified
[ ] Database abstraction verified where required
[ ] Redis verified where required
[ ] RabbitMQ verified where required
[ ] HTTP communication verified
[ ] gRPC verified
[ ] Retry verified
[ ] Timeout verified
[ ] Circuit breaker verified
[ ] Outbox verified where required
[ ] Background workers verified
[ ] Scheduled jobs verified
[ ] Health checks verified
[ ] OpenTelemetry verified
[ ] Structured logging verified
[ ] Runtime error logs verified
[ ] Exception logs verified
[ ] Query logs verified
[ ] Audit logging verified
[ ] API documentation verified
[ ] Unit tests pass
[ ] Integration tests pass
[ ] Load tests available
[ ] k6 tests available
[ ] JMeter tests available
[ ] NBomber tests available where applicable
[ ] Docker build verified
[ ] Graceful shutdown verified
[ ] CI/CD verified
[ ] Migration documentation verified
[ ] Programmer Guide updated
[ ] Security review completed
[ ] Git commit created
```

---

# 95. FINAL RULE

`node.md` defines the common Node.js platform architecture.

This document defines **Express implementation details**.

Do not duplicate framework-neutral rules unnecessarily.

Do not redesign the platform merely because Express has a different implementation model.

The final architecture should remain:

```text
Enterprise Rules
      ↓
Node.js Rules
      ↓
Express Rules
      ↓
Service Implementation
```

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
Multi-tenant ready
Communication-provider independent
Database-provider independent where required
```

# END OF NODE.JS + EXPRESS ENGINEERING RULES
