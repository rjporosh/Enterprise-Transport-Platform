# Node.js Backend & Runtime Engineering Rules

## 1. Purpose

This document defines the common engineering rules for Node.js applications regardless of the web framework.

It applies to:

* Node.js backend services
* Express
* Fastify
* NestJS
* Next.js server-side code
* Workers
* CLI services
* Background processors
* Event consumers
* API/BFF services

Framework-specific rules belong in separate documents.

For example:

```text
.ai/backend/node.md
.ai/backend/node-express.md
.ai/backend/node-nextjs.md
```

This document must NOT contain framework-specific implementation assumptions.

---

# 2. VERSION POLICY

Always use the latest stable and supported Node.js version available at implementation time.

Do not permanently hardcode an old Node.js version in this document.

Before implementation inspect:

```text
package.json
package-lock.json
pnpm-lock.yaml
yarn.lock
.nvmrc
.node-version
Dockerfile
CI/CD configuration
```

Determine the actual runtime and dependency requirements of the project.

Prefer:

```text
Latest Supported Node.js
+
Compatible TypeScript
+
Compatible Framework
+
Compatible Dependencies
```

Do not use:

```text
EOL Node.js
Nightly builds
Unstable releases
Experimental releases
```

for production unless explicitly required.

---

# 3. PACKAGE MANAGER

Respect the existing project:

```text
npm
pnpm
yarn
```

Do not change package managers without a justified architectural reason.

Use the existing lockfile.

Never manually edit lockfiles unless absolutely necessary.

---

# 4. TYPESCRIPT

TypeScript is preferred for enterprise Node.js applications.

Use:

```text
Strict typing
Interfaces
Types
Generics
Discriminated unions
DTOs
Explicit contracts
```

Prefer:

```typescript
unknown
```

over:

```typescript
any
```

when the runtime type is genuinely unknown.

Avoid unnecessary type assertions.

Do not weaken compiler strictness merely to hide implementation problems.

---

# 5. TYPESCRIPT CONFIGURATION

Where appropriate enable strict mode:

```json
{
  "compilerOptions": {
    "strict": true
  }
}
```

Also review:

```text
noImplicitAny
strictNullChecks
noUncheckedIndexedAccess
noImplicitReturns
noUnusedLocals
noUnusedParameters
```

Use settings appropriate to the existing project rather than blindly changing everything.

---

# 6. ARCHITECTURAL PRINCIPLE

The Node.js runtime must remain independent from business logic.

Prefer:

```text
Interface / API
       ↓
Application
       ↓
Domain
       ↓
Infrastructure
```

The exact folder names may differ.

The principle is:

```text
Business Logic
        ↓
must not depend unnecessarily on
        ↓
HTTP / Express / Next.js / Database / RabbitMQ / Redis
```

Framework-specific code belongs at the application boundary.

---

# 7. CLEAN ARCHITECTURE

Where appropriate use:

```text
Domain
Application
Infrastructure
Interface
```

Possible structure:

```text
src/
├── domain/
├── application/
├── infrastructure/
├── interfaces/
└── shared/
```

Do not create architecture purely for ceremony.

For small components, prefer KISS.

For complex enterprise services, maintain clear separation of concerns.

---

# 8. DOMAIN

Domain code contains business concepts and rules.

Avoid coupling domain code directly to:

```text
HTTP
Express
Next.js
Database
ORM
RabbitMQ
Redis
Cloud SDKs
File System
```

The domain should remain reusable.

---

# 9. APPLICATION LAYER

Application services/use cases orchestrate business operations.

They may coordinate:

```text
Repositories
Domain Services
External Service Interfaces
Event Publishers
Transactions
Authorization
```

but should not become giant god classes.

Keep each use case focused.

---

# 10. INFRASTRUCTURE

Infrastructure contains implementations for external technologies:

```text
Database
ORM
Redis
RabbitMQ
HTTP Clients
gRPC Clients
File Storage
Email
SMS
Push Providers
External APIs
Telemetry
```

Application/domain code should depend on abstractions where provider replacement is required.

---

# 11. DEPENDENCY INJECTION

Node.js does not require a DI framework.

Prefer explicit dependency injection where appropriate:

```typescript
const service = new PaymentService(
    paymentRepository,
    eventPublisher,
    notificationClient
);
```

If the project already uses a DI container, follow its conventions.

Do not introduce a DI container just because another language/framework uses one.

---

# 12. DATABASE ABSTRACTION

Follow:

```text
.ai/MASTER-RULE.md
.ai/AI_RULES.md
```

Where database provider switching is a requirement, use an abstraction/factory/provider pattern.

Potential providers:

```text
postgres
mysql
sqlserver
oracle
sqlite
mongodb
```

Example conceptual configuration:

```text
DatabaseProvider=postgres
```

or:

```text
DatabaseProvider=mysql
```

Business logic must not contain provider-specific branching everywhere.

Provider-specific differences belong behind the database abstraction.

Do not pretend relational and document databases have identical capabilities.

---

# 13. ORM / DATA ACCESS

Use the project's existing data-access technology.

Possible technologies include:

```text
Prisma
TypeORM
Drizzle
Sequelize
Mongoose
Knex
Native Driver
```

Do not introduce multiple ORMs without a strong reason.

---

# 14. DATABASE RULES

Queries must be:

```text
Parameterized
Bounded
Efficient
Observable
Secure
```

Avoid:

```text
N+1 queries
Unbounded queries
Full table scans
Repeated identical queries
Huge object graphs
Unnecessary joins
```

Use:

```text
Projection
Pagination
Indexes
Batching
Caching
```

where appropriate.

---

# 15. TRANSACTIONS

Use transactions when multiple database changes must succeed atomically.

Keep transaction boundaries short.

Do not perform slow external HTTP/RPC operations inside database transactions unless explicitly required.

---

# 16. CONCURRENCY

Critical mutable entities should support concurrency protection.

Possible strategies:

```text
Version
Revision
UpdatedAt
Database row version
Optimistic locking
```

Never silently overwrite concurrent changes.

---

# 17. SOFT DELETE

Where required, use a consistent strategy:

```text
isDeleted
deletedAt
deletedBy
```

Normal queries should exclude deleted records.

Administrative recovery must require authorization.

---

# 18. PAGINATION

Large collections must be paginated.

Possible models:

```text
page
pageSize
total
items
```

or:

```text
cursor
limit
items
nextCursor
```

Always enforce a maximum page size.

Never expose unlimited database reads.

---

# 19. FILTERING AND SEARCH

Filtering/search must be:

```text
Validated
Parameterized
Bounded
Indexed where appropriate
```

Never concatenate raw user input into database queries.

---

# 20. RESULT PATTERN

Use the centralized Result Pattern defined by:

```text
.ai/MASTER-RULE.md
.ai/AI_RULES.md
```

A response may contain multiple errors.

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

Do not expose raw stack traces to clients.

---

# 21. CENTRALIZED ERROR HANDLING

All applications must have centralized error handling.

The implementation depends on the framework.

Examples:

```text
Express
→ Error Middleware

Next.js
→ Framework-specific error boundaries / route handling

Worker
→ Worker-level centralized error handler
```

The underlying error model must remain consistent.

Every error should be classified where practical:

```text
Validation
Authentication
Authorization
NotFound
Conflict
Concurrency
DependencyUnavailable
Timeout
RateLimited
Unexpected
```

---

# 22. GRACEFUL ERROR MESSAGES

Clients should receive safe, useful messages.

Example:

```text
Notification service is temporarily unavailable.
```

rather than:

```text
ECONNREFUSED 10.0.0.5:5432
password authentication failed...
```

Technical details belong in logs.

API responses should include:

```text
Error Code
Human-readable Message
TraceId
CorrelationId
Errors[]
```

where applicable.

---

# 23. LOCALIZATION

Applications must support centralized localization where user-facing messages require it.

Minimum:

```text
English
Bangla
```

The architecture must allow additional languages later.

Never scatter hardcoded translated messages throughout business logic.

Use a centralized message/translation abstraction.

---

# 24. AUTHENTICATION

Use the centralized authentication architecture.

Possible mechanisms:

```text
JWT
OAuth2
OpenID Connect
Identity Provider
Session
```

Do not create independent authentication implementations in every service.

Authentication should be reusable across:

```text
HRM
ERP
HMS
Payroll
POS
Booking
Ticketing
Payment
Notification
SaaS
```

where applicable.

---

# 25. AUTHORIZATION

Support where applicable:

```text
Role
Permission
Module
Tenant
Company
Organization
Resource
```

Authorization must be enforced server-side.

Frontend authorization is not a security boundary.

---

# 26. MULTI-TENANCY

For SaaS systems preserve:

```text
TenantId
CompanyId
OrganizationId
UserId
```

through the request/application context.

Tenant information must originate from trusted authentication/infrastructure.

Never trust an arbitrary tenant ID supplied by the client.

Every tenant-scoped query must enforce tenant isolation.

---

# 27. AUDIT LOGGING

Critical actions should be auditable.

Record where applicable:

```text
UserId
TenantId
CompanyId
OrganizationId
Action
Entity
EntityId
Timestamp
CorrelationId
TraceId
Result
```

Never log:

```text
Password
OTP
JWT
Refresh Token
API Key
Secrets
Database Password
Payment Credentials
```

---

# 28. HTTP COMMUNICATION ABSTRACTION

Internal/external HTTP communication must be centralized where reuse is required.

The abstraction should support:

```text
Timeout
Retry
Circuit Breaker
CorrelationId
TraceId
Authentication
Idempotency-Key
Rate Limiting
```

Possible implementations:

```text
fetch
undici
axios
framework-specific HTTP client
```

The business layer must not depend directly on a particular HTTP client if provider switching is required.

---

# 29. gRPC

Support gRPC as an interchangeable internal communication mechanism where required.

Business logic should depend on an abstraction rather than directly on gRPC generated clients.

Support:

```text
Deadline
Cancellation
Metadata
Authentication
CorrelationId
TraceId
Retry
Timeout
Circuit Breaker
```

Keep `.proto` contracts versioned and backward compatible.

---

# 30. EVENT-DRIVEN COMMUNICATION

Support event-driven communication through the platform communication abstraction.

Possible provider:

```text
RabbitMQ
```

Additional providers may be introduced later without rewriting business logic.

Events must represent meaningful business events.

Examples:

```text
PaymentCompleted
TicketIssued
NotificationRequested
UserRegistered
OrderCreated
```

---

# 31. RABBITMQ

Where RabbitMQ is used, support as appropriate:

```text
Publisher Confirm
Acknowledgement
Retry
Dead Letter Queue
Outbox
Inbox
Idempotency
CorrelationId
TraceId
```

Never silently swallow message publishing failures.

---

# 32. OUTBOX PATTERN

For critical events:

```text
Business Transaction
        ↓
Outbox Record
        ↓
Commit
        ↓
Background Publisher
        ↓
Message Broker
```

This prevents business state from being committed while event publication fails.

---

# 33. INBOX / IDEMPOTENCY

Consumers must be idempotent.

Store appropriate message processing state:

```text
MessageId
EventId
Consumer
ProcessedAt
Result
```

Duplicate events must not produce duplicate business effects.

---

# 34. IDEMPOTENCY KEY

Critical write APIs should support:

```text
Idempotency-Key
```

where required.

Define:

```text
Scope
Storage
Expiration
Conflict Behavior
Replay Behavior
```

Idempotency must be real; merely accepting the header is insufficient.

---

# 35. RETRY

Retries must be:

```text
Bounded
Selective
Exponential
Observable
```

Use jitter where appropriate.

Do not retry permanent failures indefinitely.

Do not retry:

```text
Validation Errors
Authentication Errors
Authorization Errors
Permanent Business Errors
```

unless explicitly justified.

---

# 36. CIRCUIT BREAKER

Use circuit breakers for unstable external dependencies where appropriate.

States:

```text
Closed
Open
Half-Open
```

Circuit breakers prevent cascading failures.

Always combine them with sensible timeouts.

---

# 37. TIMEOUTS

Every external network dependency must have an explicit timeout.

Never rely indefinitely on network defaults.

Timeout values must reflect actual service requirements.

---

# 38. RATE LIMITING

Where required support rate limiting by:

```text
IP
User
Tenant
Company
Endpoint
API Key
```

Rate limiting must work correctly behind:

```text
YARP
Ocelot
Reverse Proxy
Load Balancer
Ingress
```

Only trust forwarded client IP information from trusted infrastructure.

---

# 39. CORRELATION ID

Every request should support:

```text
CorrelationId
TraceId
```

If a valid incoming correlation ID exists, propagate it according to platform rules.

Otherwise generate one.

Propagate across:

```text
HTTP
gRPC
RabbitMQ
Background Jobs
Logs
Telemetry
```

---

# 40. IP TRACE

When client IP tracking is required:

```text
Client
 ↓
Gateway
 ↓
Load Balancer
 ↓
Node Application
```

must be handled using trusted proxy configuration.

Never blindly trust arbitrary:

```text
X-Forwarded-For
X-Real-IP
```

headers from clients.

---

# 41. OBSERVABILITY

Follow:

```text
.ai/observability.md
```

Use:

```text
OpenTelemetry
Structured Logging
Metrics
Distributed Tracing
Health Checks
```

where configured.

Instrument:

```text
HTTP
Database
Redis
RabbitMQ
gRPC
Background Jobs
External APIs
```

where applicable.

---

# 42. LOGGING

Use structured logging.

Possible libraries:

```text
Pino
Winston
```

Use the existing project standard.

Production logs should preferably be machine-readable.

Avoid using:

```text
console.log()
```

as the primary production logging strategy.

---

# 43. RUNTIME ERROR LOGS

Follow:

```text
.ai/observability.md
```

Where configured:

```text
logs/runtime-error-logs/
```

Runtime failures should capture:

```text
Timestamp
Service
Entry Point
Endpoint
Background Job
Method
File
File Location
Line Number
Root Cause
Exact Exception Message
Possible Solution
Best Practice
CorrelationId
TraceId
```

Never log secrets.

---

# 44. EXCEPTION LOGS

Maintain:

```text
logs/exception-logs/
```

Expected information:

```text
Timestamp
Entry Point
Endpoint Name
Background Service / Job
Method Name
File Name
File Location
Line Number
Root Cause
Exact Exception Message
Possible Solution
Best Practice
CorrelationId
TraceId
```

The purpose is to allow developers to locate and fix problems quickly.

---

# 45. BUILD ERROR LOGS

Where configured:

```text
logs/build-errors/
```

Expected information:

```text
Timestamp
Project
Command
File
Line
Column
Exact Error
Root Cause
Possible Solution
Suggested Fix
```

Example:

```text
logs/build-errors/build-error-dd-mm-yy.txt
```

---

# 46. QUERY LOGS

Where database query diagnostics are enabled:

```text
logs/query-logs/
```

Record:

```text
Timestamp
Service
Endpoint
Method
File
File Location
Line
Database Provider
Generated Query
Started At
Ended At
Total Execution Time
CorrelationId
TraceId
```

Possible providers:

```text
PostgreSQL
MySQL
SQL Server
Oracle
SQLite
MongoDB
```

Sensitive query parameters must be masked.

---

# 47. SECURITY

Every Node.js application must consider:

```text
Input Validation
Authentication
Authorization
Tenant Isolation
Rate Limiting
CORS
Security Headers
TLS
Secrets Management
Dependency Security
Parameterized Queries
Audit Logging
```

Never disable security controls merely to simplify development.

---

# 48. CORS

CORS must be explicitly configured.

Avoid unrestricted production configuration such as:

```text
*
```

for authenticated APIs unless intentionally required.

Use trusted origins.

---

# 49. SECURITY HEADERS

Use appropriate security headers according to the framework/application.

Do not blindly apply policies that break legitimate functionality.

Review:

```text
CSP
HSTS
X-Content-Type-Options
Frame Protection
Referrer Policy
CORS
```

---

# 50. SECRETS

Never commit:

```text
.env
Passwords
API Keys
JWT Secrets
Private Keys
Database Credentials
Broker Credentials
Cloud Credentials
```

Use:

```text
Environment Variables
Secret Manager
Vault
Cloud Secret Store
CI/CD Secret Store
```

according to deployment architecture.

---

# 51. BACKGROUND WORKERS

Node applications may contain:

```text
Queue Consumers
Workers
Scheduled Jobs
Event Consumers
Background Processors
```

All background processing must support where applicable:

```text
Retry
Idempotency
Concurrency Control
Graceful Shutdown
Logging
Metrics
Failure Handling
```

---

# 52. SCHEDULED JOBS

Use the project's established scheduler.

Possible technologies:

```text
node-cron
BullMQ
Agenda
External Scheduler
Platform Scheduler
```

Document:

```text
Job Name
Purpose
Cron Expression
Timezone
Concurrency
Retry
Failure Handling
Manual Execution
Monitoring
```

Do not introduce multiple schedulers without reason.

---

# 53. GRACEFUL SHUTDOWN

Applications and workers must handle:

```text
SIGTERM
SIGINT
```

where applicable.

Preferred shutdown sequence:

```text
Stop accepting new work
        ↓
Finish active requests
        ↓
Stop workers/consumers
        ↓
Close broker connections
        ↓
Close Redis
        ↓
Close database connections
        ↓
Flush telemetry/logs
        ↓
Exit
```

Do not terminate immediately during normal deployment shutdown.

---

# 54. EVENT LOOP

Never intentionally block the Node.js event loop.

Be careful with:

```text
CPU-heavy algorithms
Large JSON processing
Large file processing
Synchronous file I/O
Image processing
Compression
Cryptographic workloads
```

Use worker threads or dedicated workers when appropriate.

---

# 55. FILE HANDLING

For uploaded files:

```text
Validate size
Validate MIME type
Validate extension
Sanitize filename
Prevent path traversal
Scan where required
Store safely
```

Never trust client-provided filenames or MIME types.

---

# 56. MEMORY

Avoid:

```text
Unbounded arrays
Unbounded caches
Large in-memory result sets
Global mutable state
Memory leaks
```

Use streaming/pagination for large datasets.

---

# 57. CONNECTION POOLS

Monitor:

```text
Pool Size
Active Connections
Idle Connections
Timeouts
Failures
Leaks
```

Do not blindly increase pool sizes.

Consider:

```text
Traffic
Database Capacity
Instance Count
Query Duration
Concurrency
```

---

# 58. PERFORMANCE

Investigate:

```text
Slow Queries
N+1
Large Payloads
Event Loop Blocking
Memory Growth
Slow External APIs
Connection Pool Exhaustion
Excessive Serialization
```

Do not optimize based on guesswork.

Measure first.

---

# 59. API GATEWAY

Services may operate behind:

```text
YARP
Ocelot
Reverse Proxy
Ingress
API Gateway
```

The application must correctly handle trusted:

```text
Client IP
CorrelationId
TraceId
Authentication Context
```

Do not assume direct internet exposure.

---

# 60. COMMUNICATION FACTORY

Where provider switching is required, use an abstraction/factory pattern.

Conceptually:

```text
CommunicationProvider
        ↓
 ┌───────────────┐
 │ HTTP          │
 │ gRPC          │
 │ RabbitMQ      │
 │ EventDriven   │
 └───────────────┘
```

Business logic should depend on:

```text
IServiceCommunicator
IEventPublisher
```

or equivalent abstractions rather than provider-specific implementations.

Changing:

```text
CommunicationProvider=http
```

to:

```text
CommunicationProvider=grpc
```

should not require rewriting business logic.

---

# 61. HEALTH CHECKS

Expose appropriate:

```text
Liveness
Readiness
Dependency Health
```

A process can be alive while a dependency is unavailable.

Do not expose secrets through health endpoints.

---

# 62. API DOCUMENTATION

Maintain OpenAPI documentation where applicable.

Document:

```text
Endpoints
Requests
Responses
Authentication
Authorization
Errors
Pagination
Filtering
Idempotency
Rate Limits
```

Keep documentation synchronized with the actual implementation.

---

# 63. TESTING

Follow:

```text
.ai/testing-and-performance.md
```

Use the project's chosen test stack.

Possible tools:

```text
Vitest
Jest
Node Test Runner
Supertest
Testcontainers
```

Do not introduce multiple overlapping frameworks without reason.

---

# 64. UNIT TESTS

Unit tests should cover:

```text
Domain Rules
Application Services
Validators
Business Rules
Error Mapping
Critical Logic
```

Tests must be deterministic.

---

# 65. INTEGRATION TESTS

Test where applicable:

```text
API
Database
Transactions
Authentication
Authorization
RabbitMQ
Redis
External Services
Error Handling
```

Use realistic infrastructure where practical.

---

# 66. LOAD AND PERFORMANCE TESTS

Every production-grade service must support the platform performance strategy.

Maintain where required:

```text
tests/load-test/
```

Support:

```text
NBomber
k6
Apache JMeter
```

according to:

```text
.ai/testing-and-performance.md
```

Document:

```text
How to run
Environment variables
Target URL
Authentication
Virtual Users
Ramp-up
Duration
Expected Results
Result Location
```

---

# 67. CONTRACT TESTING

Where services communicate using:

```text
HTTP
gRPC
RabbitMQ
```

verify contracts.

Breaking contract changes require:

```text
Versioning
Compatibility
Migration Strategy
```

---

# 68. DOCKER

Use production-appropriate Node.js runtime images.

Prefer multi-stage builds:

```text
Build Stage
    ↓
Production Runtime
```

Production images must contain only required runtime dependencies.

Never place secrets inside Docker images.

---

# 69. CI/CD

CI/CD should verify applicable:

```text
Install
Lint
Type Check
Build
Unit Tests
Integration Tests
Security Scan
Dependency Audit
Docker Build
```

Use the actual project scripts.

Example:

```bash
npm ci
npm run lint
npm run typecheck
npm test
npm run build
```

Do not invent scripts that do not exist.

---

# 70. DEPENDENCY MANAGEMENT

Before adding a package:

1. Check existing dependencies.
2. Check whether Node.js/framework already provides the capability.
3. Check maintenance status.
4. Check compatibility.
5. Check vulnerabilities.
6. Check license implications where relevant.
7. Avoid unnecessary dependencies.

Keep dependency count reasonable.

---

# 71. CODE QUALITY

Before commit inspect:

```text
Type Safety
Dead Code
Unused Dependencies
Duplicate Logic
Circular Dependencies
Error Handling
Security
Concurrency
Database Queries
Logging
Performance
Resource Cleanup
```

Avoid giant files.

Avoid god classes.

Avoid god services.

---

# 72. NO SILENT CATCH

Never write:

```typescript
try {
    // ...
} catch {
}
```

without a deliberate reason.

Every caught exception must be:

```text
Handled
Translated
Logged
Rethrown
```

or intentionally ignored with documented justification.

---

# 73. NO RAW PROCESS EXIT

Do not use:

```typescript
process.exit()
```

as normal business error handling.

Startup configuration failures may terminate intentionally.

Runtime business errors must use the centralized error architecture.

---

# 74. DOCUMENTATION

Maintain:

```text
docs/programmers-guide/
```

where required.

Documentation should cover:

```text
Service Architecture
Folder Structure
CRUD
Entity Creation
CQRS
Validation
Repository
Database
Migrations
Background Workers
Scheduled Jobs
Cron
HTTP Communication
gRPC
RabbitMQ
Events
Testing
Load Testing
Troubleshooting
Deployment
```

Keep documentation concise and developer-friendly.

---

# 75. MIGRATIONS

Migration commands must match the actual data-access technology.

Do not invent commands.

Examples may differ for:

```text
Prisma
TypeORM
Drizzle
Sequelize
MongoDB
```

The repository's actual tooling is authoritative.

---

# 76. BUILD VERIFICATION

Before committing:

```text
Install
 ↓
Type Check
 ↓
Lint
 ↓
Build
 ↓
Unit Tests
 ↓
Integration Tests
```

where applicable.

Fix implementation errors before committing.

---

# 77. DEFINITION OF DONE

A framework-neutral Node.js feature is complete when applicable:

```text
[ ] Implementation complete
[ ] Type checking passes
[ ] Lint passes
[ ] Build passes
[ ] Unit tests pass
[ ] Integration tests pass
[ ] Validation implemented
[ ] Result Pattern implemented
[ ] Centralized errors implemented
[ ] Localization implemented
[ ] Authentication verified
[ ] Authorization verified
[ ] Tenant isolation verified
[ ] Database verified
[ ] Migrations verified
[ ] HTTP communication verified
[ ] gRPC verified
[ ] RabbitMQ verified
[ ] Idempotency verified
[ ] Retry verified
[ ] Timeout verified
[ ] Circuit breaker verified
[ ] Rate limiting verified
[ ] Audit logging verified
[ ] Exception logging verified
[ ] Query logging verified
[ ] OpenTelemetry verified
[ ] Health checks verified
[ ] Graceful shutdown verified
[ ] Docker verified
[ ] CI/CD verified
[ ] Load tests added where required
[ ] Documentation updated
[ ] Professional Git commit created
```

---

# 78. FRAMEWORK-SPECIFIC EXTENSION

This file defines the common Node.js rules.

Framework-specific behavior must be documented separately.

```text
.ai/backend/node.md
        ↓
 ┌───────────────────────┐
 │                       │
 ↓                       ↓
node-express.md     node-nextjs.md
```

`node-express.md` must contain Express-specific:

```text
Middleware
Router
Controller
Request/Response
Express Error Middleware
Express Security
Express Lifecycle
```

`node-nextjs.md` must contain Next.js-specific:

```text
App Router
Server Components
Client Components
Route Handlers
Server Actions
SSR
SSG
ISR
Caching
Revalidation
Middleware
BFF
React Integration
```

Do not duplicate the common Node.js rules unnecessarily.

---

# 79. FINAL PRINCIPLE

Do not make Node.js imitate:

```text
.NET
Java
Python
```

Use Node.js strengths:

```text
Asynchronous I/O
Event-driven architecture
TypeScript
Streaming
Workers
High-concurrency networking
```

The implementation must remain:

```text
Correct
Secure
Reliable
Observable
Performant
Scalable
Maintainable
Testable
Reusable
Production-ready
Enterprise-grade
```

# END OF NODE.JS COMMON ENGINEERING RULES
